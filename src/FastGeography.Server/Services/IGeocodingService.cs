namespace FastGeography.Server.Services;

using FastGeography.Shared;

/// <summary>
/// Validates a player's geography answer against an external geocoding provider.
/// </summary>
public interface IGeocodingService
{
    /// <summary>
    /// Returns a <see cref="GeocodeResult"/> with the awarded points and, when valid,
    /// the geographic coordinates of the matched place.
    /// </summary>
    Task<GeocodeResult> ValidateAsync(
        string location,
        LocationType locationType,
        CancellationToken cancellationToken = default);
}
