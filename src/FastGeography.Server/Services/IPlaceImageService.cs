namespace FastGeography.Server.Services;

using FastGeography.Shared;

/// <summary>
/// Resolves a thumbnail image for a verified place (Wikipedia / Wikimedia Commons).
/// Results are cached on <see cref="Data.Entities.Toponym"/> rows.
/// </summary>
public interface IPlaceImageService
{
    Task<PlaceImageResult?> GetImageAsync(
        string normalizedName,
        LocationType type,
        string displayName,
        double? latitude,
        double? longitude,
        GameLanguage lang,
        CancellationToken ct = default);
}

/// <summary>A resolved place thumbnail and its source attribution.</summary>
public sealed record PlaceImageResult(string ImageUrl, string Attribution);
