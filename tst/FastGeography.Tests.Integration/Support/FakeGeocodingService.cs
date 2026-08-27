namespace FastGeography.IntegrationTests.Support;

using FastGeography.Server.Services;
using FastGeography.Shared;

/// <summary>
/// Deterministic, network-free substitute for <see cref="IGeocodingService"/>.
/// Returns <see cref="ScoringRules.ValidPoints"/> for a fixed set of known places
/// and <see cref="ScoringRules.InvalidPoints"/> for everything else.
/// </summary>
internal sealed class FakeGeocodingService : IGeocodingService
{
    private static readonly HashSet<string> KnownPlaces = new(StringComparer.OrdinalIgnoreCase)
    {
        "london", "paris", "berlin", "sofia", "tokyo"
    };

    public Task<GeocodeResult> ValidateAsync(
        string location,
        LocationType locationType,
        CancellationToken cancellationToken = default)
    {
        var known = KnownPlaces.Contains(location);

        return Task.FromResult(new GeocodeResult
        {
            LocationType = locationType,
            Points = known ? ScoringRules.ValidPoints : ScoringRules.InvalidPoints,
            Coordinates = known ? "51.5074,-0.1278" : null
        });
    }
}
