namespace FastGeography.Server.Services;

using System.Globalization;

using FastGeography.Server.Data;
using FastGeography.Server.Data.Entities;
using FastGeography.Server.Options;
using FastGeography.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>
/// Decorator over the configured geocoding back-end that caches verified toponyms in
/// the database so repeated lookups skip the external API entirely.
///
/// Lookup order:
///   1. <c>Toponyms</c> table (exact match on normalised name + category).
///   2. Active geocoding provider.  On a valid result the row is inserted for future calls.
///   3. Invalid provider responses are never persisted.
/// </summary>
public sealed class CatalogGeocodingService : IGeocodingService
{
    private readonly IGeocodingService _inner;
    private readonly string _providerName;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CatalogGeocodingService> _logger;

    public CatalogGeocodingService(
        [FromKeyedServices("active")] IGeocodingService inner,
        IOptions<GeocodingOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<CatalogGeocodingService> logger)
    {
        _inner = inner;
        _providerName = options.Value.Provider;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<GeocodeResult> ValidateAsync(
        string location,
        LocationType locationType,
        CancellationToken cancellationToken = default)
    {
        var key = Normalize(location);

        // --- 1. Catalog lookup (own scope so parallel WhenAll calls don't share a context) ---
        await using (var readScope = _scopeFactory.CreateAsyncScope())
        {
            var db = readScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var entry = await db.Toponyms
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.NormalizedName == key && t.Category == locationType,
                    cancellationToken);

            if (entry is not null)
            {
                _logger.LogDebug(
                    "Catalog hit for {Location}/{LocationType}", location, locationType);

                return new GeocodeResult
                {
                    LocationType = locationType,
                    Points = ScoringRules.ValidPoints,
                    Coordinates = FormatCoordinates(entry.Latitude, entry.Longitude)
                };
            }
        }

        // --- 2. Fall back to the configured geocoding provider ---
        var result = await _inner.ValidateAsync(location, locationType, cancellationToken);

        // --- 3. Persist verified results for future calls ---
        if (result.Points == ScoringRules.ValidPoints && result.Coordinates is not null)
        {
            await TryPersistAsync(key, location.Trim(), locationType, result.Coordinates, cancellationToken);
        }

        return result;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task TryPersistAsync(
        string normalizedName,
        string displayName,
        LocationType category,
        string coordinates,
        CancellationToken cancellationToken)
    {
        if (!TryParseCoordinates(coordinates, out var lat, out var lon))
        {
            _logger.LogWarning(
                "Could not parse coordinates '{Coordinates}' for toponym '{Name}' — skipping persist",
                coordinates, displayName);
            return;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.Toponyms.Add(new Toponym
            {
                Id = Guid.NewGuid(),
                NormalizedName = normalizedName,
                DisplayName = displayName,
                Category = category,
                Latitude = lat,
                Longitude = lon,
                Provider = _providerName,
                VerifiedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Persisted new verified toponym '{DisplayName}' ({Category}) at {Lat},{Lon}",
                displayName, category, lat, lon);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Two concurrent validations for the same key both missed the catalog and
            // both tried to insert.  The second one loses — that is fine.
            _logger.LogDebug(
                "Toponym '{Name}'/{Category} already inserted by a concurrent request",
                displayName, category);
        }
    }

    private static string Normalize(string name) =>
        name.Trim().ToLowerInvariant();

    private static string FormatCoordinates(double lat, double lon) =>
        $"{lat.ToString(CultureInfo.InvariantCulture)},{lon.ToString(CultureInfo.InvariantCulture)}";

    private static bool TryParseCoordinates(string coordinates, out double lat, out double lon)
    {
        lat = lon = 0;
        var parts = coordinates.Split(',');
        return parts.Length == 2
            && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out lat)
            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out lon);
    }

    /// <summary>
    /// Detects unique-constraint violations for both PostgreSQL (error code 23505)
    /// and SQLite / InMemory (the latter never raises this — guard is harmless).
    /// </summary>
    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("23505", StringComparison.Ordinal) == true
        || ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true;
}
