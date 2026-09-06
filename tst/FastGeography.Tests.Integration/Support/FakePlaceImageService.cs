namespace FastGeography.IntegrationTests.Support;

using FastGeography.Server.Services;
using FastGeography.Shared;

/// <summary>
/// Test double that skips Wikipedia / Commons HTTP calls.
/// </summary>
public sealed class FakePlaceImageService : IPlaceImageService
{
    public Task<PlaceImageResult?> GetImageAsync(
        string normalizedName,
        LocationType type,
        string displayName,
        double? latitude,
        double? longitude,
        GameLanguage lang,
        CancellationToken ct = default) =>
        Task.FromResult<PlaceImageResult?>(null);
}
