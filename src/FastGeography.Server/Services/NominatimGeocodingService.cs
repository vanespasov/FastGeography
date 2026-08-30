namespace FastGeography.Server.Services;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

using FastGeography.Server.Options;
using FastGeography.Shared;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

/// <summary>
/// Geocoding adapter backed by Nominatim (OpenStreetMap).
/// Public instance policy: max 1 request per second, must set a descriptive User-Agent.
/// See https://operations.osmfoundation.org/policies/nominatim/
/// </summary>
public sealed class NominatimGeocodingService : IGeocodingService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Ensures at most one Nominatim call per <see cref="MinRequestInterval"/>.
    /// Instance field (service is singleton) – no need for static.
    /// </summary>
    private readonly SemaphoreSlim _throttle = new(1, 1);
    private DateTime _lastCallAt = DateTime.MinValue;

    private static readonly TimeSpan MinRequestInterval = TimeSpan.FromMilliseconds(1100);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NominatimGeocodingService> _logger;

    public NominatimGeocodingService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<NominatimGeocodingService> logger)
    {
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
        var langCode = language.ToCode();
        var cacheKey = $"geocode:nominatim:{langCode}:{location.Trim().ToLowerInvariant()}:{locationType}";

        if (_cache.TryGetValue(cacheKey, out GeocodeResult? cached) && cached is not null)
        {
            _logger.LogDebug("Nominatim cache hit for {Location}/{LocationType}/{Language}", location, locationType, langCode);
            return cached;
        }

        var result = await CallNominatimAsync(location, locationType, langCode, cancellationToken);

        _cache.Set(cacheKey, result, CacheTtl);
        return result;
    }

    private async Task<GeocodeResult> CallNominatimAsync(
        string location,
        LocationType locationType,
        string langCode,
        CancellationToken cancellationToken)
    {
        try
        {
            await ThrottleAsync(cancellationToken);

            var client = _httpClientFactory.CreateClient("nominatim");
            var url = $"search?q={Uri.EscapeDataString(location)}&format=jsonv2&limit=5&addressdetails=1&accept-language={Uri.EscapeDataString(langCode)}";

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(RequestTimeout);

            // Also send Accept-Language header for providers that honour it.
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("Accept-Language", langCode);

            using var response = await client.SendAsync(request, cts.Token);
            response.EnsureSuccessStatusCode();

            var results = await response.Content.ReadFromJsonAsync<NominatimResult[]>(cts.Token);

            if (results is not { Length: > 0 })
            {
                _logger.LogInformation(
                    "Nominatim returned no results for {Location}/{LocationType}", location, locationType);
                return Invalid(locationType);
            }

            var match = results.FirstOrDefault(r => LocationMatchesType(r, locationType));

            if (match is null)
            {
                _logger.LogInformation(
                    "No Nominatim result for {Location} satisfies {LocationType}", location, locationType);
                return Invalid(locationType);
            }

            var coordinates = $"{match.Lat},{match.Lon}";

            _logger.LogInformation(
                "Nominatim success for {Location}/{LocationType} at {Coordinates}", location, locationType, coordinates);

            return new GeocodeResult
            {
                LocationType = locationType,
                Points = ScoringRules.ValidPoints,
                Coordinates = coordinates
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Nominatim request timed out for {Location}/{LocationType}", location, locationType);
            return Invalid(locationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nominatim request failed for {Location}/{LocationType}", location, locationType);
            return Invalid(locationType);
        }
    }

    /// <summary>
    /// Ensures at least <see cref="MinRequestInterval"/> between calls to the public
    /// Nominatim instance. The semaphore guards only the critical section of reading /
    /// updating <c>_lastCallAt</c> so the actual HTTP call is not serialised.
    /// </summary>
    private async Task ThrottleAsync(CancellationToken cancellationToken)
    {
        await _throttle.WaitAsync(cancellationToken);
        try
        {
            var wait = MinRequestInterval - (DateTime.UtcNow - _lastCallAt);
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, cancellationToken);
            _lastCallAt = DateTime.UtcNow;
        }
        finally
        {
            _throttle.Release();
        }
    }

    public static bool LocationMatchesType(NominatimResult result, LocationType locationType)
    {
        // jsonv2 renamed OSM "class" to "category"; accept either so both formats match.
        var featureClass = result.FeatureClass ?? string.Empty;
        var type = result.Type ?? string.Empty;
        var addressType = result.AddressType ?? string.Empty;

        return locationType switch
        {
            // Bing treats City and Village both as "PopulatedPlace"; use Nominatim's
            // addresstype which is derived from OSM place= or boundary= tags.
            LocationType.City or LocationType.Village =>
                PopulatedPlaceAddressTypes.Contains(addressType),

            LocationType.Country =>
                string.Equals(addressType, "country", StringComparison.OrdinalIgnoreCase),

            LocationType.Mountain =>
                MountainAddressTypes.Contains(addressType) ||
                (string.Equals(featureClass, "natural", StringComparison.OrdinalIgnoreCase) &&
                 MountainTypes.Contains(type)),

            LocationType.River =>
                RiverAddressTypes.Contains(addressType) ||
                (string.Equals(featureClass, "waterway", StringComparison.OrdinalIgnoreCase) &&
                 RiverTypes.Contains(type)),

            _ => false
        };
    }

    private static GeocodeResult Invalid(LocationType locationType) =>
        new() { LocationType = locationType, Points = ScoringRules.InvalidPoints };

    private static readonly HashSet<string> PopulatedPlaceAddressTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "city", "town", "village", "hamlet", "suburb", "borough",
            "quarter", "neighbourhood", "municipality", "isolated_dwelling"
        };

    private static readonly HashSet<string> MountainTypes =
        new(StringComparer.OrdinalIgnoreCase) { "peak", "ridge", "mountain_range", "hill", "volcano" };

    private static readonly HashSet<string> MountainAddressTypes =
        new(StringComparer.OrdinalIgnoreCase) { "peak", "ridge", "mountain_range", "hill", "volcano" };

    private static readonly HashSet<string> RiverTypes =
        new(StringComparer.OrdinalIgnoreCase) { "river", "stream", "canal", "drain" };

    private static readonly HashSet<string> RiverAddressTypes =
        new(StringComparer.OrdinalIgnoreCase) { "river", "stream", "canal", "drain" };
}

/// <summary>DTO for a single result from Nominatim's jsonv2 search endpoint.</summary>
public sealed class NominatimResult
{
    [JsonPropertyName("class")]
    public string? Class { get; init; }

    /// <summary>
    /// jsonv2 name for OSM class. Prefer <see cref="FeatureClass"/> when matching types.
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>OSM feature class from either json (<c>class</c>) or jsonv2 (<c>category</c>).</summary>
    public string? FeatureClass => Class ?? Category;

    /// <summary>
    /// High-level address category (e.g. "city", "country", "peak").
    /// Most reliable discriminator for geocoding because it reflects the
    /// OpenStreetMap place= / boundary= classification hierarchy.
    /// </summary>
    [JsonPropertyName("addresstype")]
    public string? AddressType { get; init; }

    [JsonPropertyName("lat")]
    public string Lat { get; init; } = string.Empty;

    [JsonPropertyName("lon")]
    public string Lon { get; init; } = string.Empty;
}
