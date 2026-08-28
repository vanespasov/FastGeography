namespace FastGeography.Server.Services;

using BingMapsRESTToolkit;

using FastGeography.Server.Options;
using FastGeography.Shared;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

public sealed class BingGeocodingService : IGeocodingService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    private readonly GeocodingOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BingGeocodingService> _logger;

    public BingGeocodingService(
        IOptions<GeocodingOptions> options,
        IMemoryCache cache,
        ILogger<BingGeocodingService> logger)
    {
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<GeocodeResult> ValidateAsync(
        string location,
        LocationType locationType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BingMaps.ApiKey))
        {
            _logger.LogWarning(
                "Bing Maps API key is not configured. Returning invalid result for {Location}/{LocationType}",
                location, locationType);
            return Invalid(locationType);
        }

        var cacheKey = $"geocode:bing:{location.Trim().ToLowerInvariant()}:{locationType}";

        if (_cache.TryGetValue(cacheKey, out GeocodeResult? cached) && cached is not null)
        {
            _logger.LogDebug("Cache hit for {Location}/{LocationType}", location, locationType);
            return cached;
        }

        var result = await CallBingAsync(location, locationType, cancellationToken);

        _cache.Set(cacheKey, result, CacheTtl);
        return result;
    }

    private async Task<GeocodeResult> CallBingAsync(
        string location,
        LocationType locationType,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new GeocodeRequest
            {
                Query = location,
                IncludeIso2 = true,
                MaxResults = 1,
                BingMapsKey = _options.BingMaps.ApiKey
            };

            var response = await request.Execute().WaitAsync(RequestTimeout, cancellationToken);

            if (!IsValidResponse(response))
            {
                _logger.LogInformation(
                    "Bing Maps returned no results for {Location}/{LocationType}", location, locationType);
                return Invalid(locationType);
            }

            var match = response.ResourceSets[0].Resources[0] as Location;

            if (!LocationMatchesType(match, locationType))
            {
                _logger.LogInformation(
                    "Bing match for {Location} entity type '{EntityType}' does not satisfy {LocationType}",
                    location, match?.EntityType, locationType);
                return Invalid(locationType);
            }

            var coordinates = $"{match!.Point.Coordinates[0]},{match.Point.Coordinates[1]}";

            _logger.LogInformation(
                "Geocode success for {Location}/{LocationType} at {Coordinates}", location, locationType, coordinates);

            return new GeocodeResult
            {
                LocationType = locationType,
                Points = ScoringRules.ValidPoints,
                Coordinates = coordinates
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Bing Maps request timed out for {Location}/{LocationType}", location, locationType);
            return Invalid(locationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bing Maps request failed for {Location}/{LocationType}", location, locationType);
            return Invalid(locationType);
        }
    }

    private static GeocodeResult Invalid(LocationType locationType) =>
        new() { LocationType = locationType, Points = ScoringRules.InvalidPoints };

    private static bool IsValidResponse(Response? response) =>
        response?.ResourceSets is { Length: > 0 } &&
        response.ResourceSets[0].Resources is { Length: > 0 };

    private static bool LocationMatchesType(Location? location, LocationType locationType)
    {
        if (location is null) return false;

        return locationType switch
        {
            LocationType.City    => location.EntityType.Contains("PopulatedPlace"),
            LocationType.Village => location.EntityType.Contains("PopulatedPlace"),
            LocationType.Country => location.EntityType.Contains("CountryRegion") ||
                                    location.EntityType.Contains("AdminDivision1"),
            LocationType.Mountain => location.EntityType.Contains("Mountain") ||
                                     location.EntityType.Contains("MountainRange"),
            LocationType.River => location.EntityType.Contains("River"),
            _ => false
        };
    }
}
