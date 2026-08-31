namespace FastGeography.Server.Services;

using FastGeography.Shared;

/// <summary>
/// Generates (or retrieves from cache) a short destination story for a verified place.
/// </summary>
public interface IDestinationStoryService
{
    /// <param name="place">Display name of the place (e.g. "Skopje").</param>
    /// <param name="type">Location category.</param>
    /// <param name="coordinates">
    ///   Latitude/longitude formatted as "lat,lon", or <c>null</c> when unavailable.
    /// </param>
    /// <param name="lang">Game language that determines the story language.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    ///   A 40–70 word story string, or <c>null</c> when no AI provider is configured
    ///   or the call fails.
    /// </returns>
    Task<string?> GetStoryAsync(
        string place,
        LocationType type,
        string? coordinates,
        GameLanguage lang,
        CancellationToken ct = default);
}
