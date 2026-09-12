namespace FastGeography.IntegrationTests.Support;

using FastGeography.Server.Services;
using FastGeography.Shared;

/// <summary>
/// Controllable stub for <see cref="IGeocodingService"/> used in integration tests.
/// Tracks call count and returns a preset <see cref="GeocodeResult"/>.
/// </summary>
internal sealed class SpyGeocodingService : IGeocodingService
{
    private GeocodeResult _next = new() { Points = ScoringRules.InvalidPoints };

    public int Calls { get; set; }

    public void Reset(bool invalidByDefault = true, int? points = null, string? coordinates = null)
    {
        Calls = 0;
        _next = new GeocodeResult
        {
            Points      = points ?? (invalidByDefault ? ScoringRules.InvalidPoints : ScoringRules.ValidPoints),
            Coordinates = coordinates
        };
    }

    public Task<GeocodeResult> ValidateAsync(
        string location, LocationType locationType,
        GameLanguage language = GameLanguage.En,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(new GeocodeResult
        {
            LocationType = locationType,
            Points       = _next.Points,
            Coordinates  = _next.Coordinates
        });
    }
}
