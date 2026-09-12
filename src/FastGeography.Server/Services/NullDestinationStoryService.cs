namespace FastGeography.Server.Services;

using FastGeography.Shared;

/// <summary>
/// No-op implementation used when no AI provider is configured.
/// Always returns <c>null</c> so the UI degrades cleanly.
/// </summary>
public sealed class NullDestinationStoryService : IDestinationStoryService
{
    public Task<string?> GetStoryAsync(
        string place,
        LocationType type,
        string? coordinates,
        GameLanguage lang,
        CancellationToken ct = default) => Task.FromResult<string?>(null);
}
