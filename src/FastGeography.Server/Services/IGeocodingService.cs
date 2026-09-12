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
    /// <param name="location">The player's typed answer.</param>
    /// <param name="locationType">Expected geography category.</param>
    /// <param name="language">Game language controlling which locale is sent to the provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<GeocodeResult> ValidateAsync(
        string location,
        LocationType locationType,
        GameLanguage language = GameLanguage.En,
        CancellationToken cancellationToken = default);
}
