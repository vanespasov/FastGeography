namespace FastGeography.IntegrationTests.Support;

using FastGeography.Server.Services;
using FastGeography.Shared;

/// <summary>
/// Test double for <see cref="IDestinationStoryService"/>.
/// Returns a predictable story string so integration tests never call OpenAI.
/// </summary>
public sealed class FakeDestinationStoryService : IDestinationStoryService
{
    public Task<string?> GetStoryAsync(
        string place,
        LocationType type,
        string? coordinates,
        GameLanguage lang,
        CancellationToken ct = default)
    {
        return Task.FromResult<string?>($"Fake story for {place} ({type}) in {lang}.");
    }
}
