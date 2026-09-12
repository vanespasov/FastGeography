namespace FastGeography.Server.Services.Ai;

/// <summary>
/// Minimal chat completion abstraction shared by all destination-story providers.
/// </summary>
public interface IChatCompletionClient
{
    Task<string?> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}
