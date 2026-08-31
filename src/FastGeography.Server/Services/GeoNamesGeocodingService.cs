namespace FastGeography.Server.Services;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

using FastGeography.Server.Options;
using FastGeography.Shared;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

/// <summary>
/// Geocoding adapter backed by GeoNames (https://www.geonames.org).
/// Requires a free username — register at https://www.geonames.org/login.
/// Free tier: ~10 000 API credits per day.
/// </summary>
public sealed class GeoNamesGeocodingService : IGeocodingService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    private readonly GeocodingOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GeoNamesGeocodingService> _logger;

    public GeoNamesGeocodingService(
        IOptions<GeocodingOptions> options,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<GeoNamesGeocodingService> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<GeocodeResult> ValidateAsync(
        string location,
        LocationType locationType,
        GameLanguage language = GameLanguage.En,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.GeoNames.Username))
        {
            _logger.LogWarning(
                "GeoNames username is not configured. Returning invalid result for {Location}/{LocationType}",
                location, locationType);
            return Invalid(locationType);
        }

        var langCode = language.ToCode();
        var cacheKey = $"geocode:geonames:{langCode}:{location.Trim().ToLowerInvariant()}:{locationType}";

        if (_cache.TryGetValue(cacheKey, out GeocodeResult? cached) && cached is not null)
        {
            _logger.LogDebug("GeoNames cache hit for {Location}/{LocationType}/{Language}", location, locationType, langCode);
            return cached;
        }

        var result = await CallGeoNamesAsync(location, locationType, langCode, cancellationToken);

        _cache.Set(cacheKey, result, CacheTtl);
        return result;
    }

    private async Task<GeocodeResult> CallGeoNamesAsync(
        string location,
        LocationType locationType,
        string langCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("geonames");
            var url = $"searchJSON?q={Uri.EscapeDataString(location)}&maxRows=5" +
                      $"&username={Uri.EscapeDataString(_options.GeoNames.Username)}" +
                      $"&lang={Uri.EscapeDataString(langCode)}";

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(RequestTimeout);

            var response = await client.GetFromJsonAsync<GeoNamesResponse>(url, cts.Token);

            if (response?.GeoNames is not { Length: > 0 })
            {
                _logger.LogInformation(
                    "GeoNames returned no results for {Location}/{LocationType}", location, locationType);
                return Invalid(locationType);
            }

            var match = response.GeoNames.FirstOrDefault(g => LocationMatchesType(g, locationType));

            if (match is null)
            {
                _logger.LogInformation(
                    "No GeoNames result for {Location} satisfies {LocationType}", location, locationType);
                return Invalid(locationType);
            }

            var coordinates = $"{match.Lat},{match.Lng}";

            _logger.LogInformation(
                "GeoNames success for {Location}/{LocationType} at {Coordinates}", location, locationType, coordinates);

            return new GeocodeResult
            {
                LocationType = locationType,
                Points = ScoringRules.ValidPoints,
                Coordinates = coordinates
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("GeoNames request timed out for {Location}/{LocationType}", location, locationType);
            return Invalid(locationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GeoNames request failed for {Location}/{LocationType}", location, locationType);
            return Invalid(locationType);
        }
    }

    /// <summary>
    /// Maps GeoNames feature class (fcl) and feature code (fcode) to <see cref="LocationType"/>.
    ///
    /// GeoNames feature classes relevant to this game:
    ///   P  – populated places  (City / Village)
    ///   A  – administrative    (Country → PCLI*, PCLF, PCLD)
    ///   T  – mountains, hills  (Mountain → MT, MTS, PK, PKS, MNTN, MNTS, HILL)
    ///   H  – streams, rivers   (River → STM*, RVN, RVRS)
    /// </summary>
    public static bool LocationMatchesType(GeoNamesGeoname geoname, LocationType locationType)
    {
        var fcl = geoname.Fcl ?? string.Empty;
        var fcode = geoname.Fcode ?? string.Empty;

        return locationType switch
        {
            // Bing treats City and Village identically as "PopulatedPlace".
            LocationType.City or LocationType.Village =>
                string.Equals(fcl, "P", StringComparison.OrdinalIgnoreCase),

            LocationType.Country =>
                string.Equals(fcl, "A", StringComparison.OrdinalIgnoreCase) &&
                CountryFcodes.Contains(fcode.ToUpperInvariant()),

            LocationType.Mountain =>
                string.Equals(fcl, "T", StringComparison.OrdinalIgnoreCase) &&
                MountainFcodes.Contains(fcode.ToUpperInvariant()),

            LocationType.River =>
                string.Equals(fcl, "H", StringComparison.OrdinalIgnoreCase) &&
                fcode.StartsWith("STM", StringComparison.OrdinalIgnoreCase) ||
                RiverFcodes.Contains(fcode.ToUpperInvariant()),

            _ => false
        };
    }

    private static GeocodeResult Invalid(LocationType locationType) =>
        new() { LocationType = locationType, Points = ScoringRules.InvalidPoints };

    private static readonly HashSet<string> CountryFcodes =
        new(StringComparer.OrdinalIgnoreCase) { "PCLI", "PCLIX", "PCLF", "PCLD", "PCLH", "PCLS" };

    private static readonly HashSet<string> MountainFcodes =
        new(StringComparer.OrdinalIgnoreCase) { "MT", "MTS", "PK", "PKS", "MNTN", "MNTS", "HILL", "HILLS", "RDGE" };

    private static readonly HashSet<string> RiverFcodes =
        new(StringComparer.OrdinalIgnoreCase) { "RVN", "RVRS" };
}

/// <summary>GeoNames searchJSON top-level response.</summary>
public sealed class GeoNamesResponse
{
    [JsonPropertyName("geonames")]
    public GeoNamesGeoname[]? GeoNames { get; init; }
}

/// <summary>A single GeoNames result entry.</summary>
public sealed class GeoNamesGeoname
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Feature class (P, A, T, H, ...).</summary>
    [JsonPropertyName("fcl")]
    public string? Fcl { get; init; }

    /// <summary>Feature code (PPLC, PCLI, MT, STM, ...).</summary>
    [JsonPropertyName("fcode")]
    public string? Fcode { get; init; }

    [JsonPropertyName("lat")]
    public string Lat { get; init; } = string.Empty;

    [JsonPropertyName("lng")]
    public string Lng { get; init; } = string.Empty;
}
