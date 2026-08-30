namespace FastGeography.Server.Options;

/// <summary>
/// Configuration for destination story AI. Bind from the "DestinationAi" config section.
/// Secrets via user-secrets or environment variables (e.g. DestinationAi__ApiKey).
/// </summary>
public sealed class DestinationAiOptions
{
    public const string Section = "DestinationAi";

    /// <summary>
    /// Active provider: None, OpenAI, Grok, Claude, or Ollama.
    /// </summary>
    public string Provider { get; set; } = "None";

    /// <summary>Chat completion model name (provider-specific).</summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>API key for cloud providers. Not required for Ollama.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional base URL override. When empty, defaults are used per provider.
    /// Ollama example: http://localhost:11434/v1
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    public DestinationAiProvider GetProvider() =>
        Enum.TryParse<DestinationAiProvider>(Provider, ignoreCase: true, out var p)
            ? p
            : DestinationAiProvider.None;

    /// <summary>Returns true when the configured provider has enough config to call an LLM.</summary>
    public bool IsConfigured()
    {
        var provider = GetProvider();
        if (provider is DestinationAiProvider.None)
            return false;

        if (provider is DestinationAiProvider.Ollama)
            return !string.IsNullOrWhiteSpace(GetEffectiveBaseUrl());

        return !string.IsNullOrWhiteSpace(ApiKey);
    }

    public string GetEffectiveBaseUrl() =>
        !string.IsNullOrWhiteSpace(BaseUrl)
            ? BaseUrl.TrimEnd('/')
            : GetProvider() switch
            {
                DestinationAiProvider.OpenAI => "https://api.openai.com/v1",
                DestinationAiProvider.Grok => "https://api.x.ai/v1",
                DestinationAiProvider.Claude => "https://api.anthropic.com",
                DestinationAiProvider.Ollama => "http://localhost:11434/v1",
                _ => string.Empty
            };

    public string GetEffectiveModel() =>
        !string.IsNullOrWhiteSpace(Model)
            ? Model
            : GetProvider() switch
            {
                DestinationAiProvider.OpenAI => "gpt-4o-mini",
                DestinationAiProvider.Grok => "grok-2-1212",
                DestinationAiProvider.Claude => "claude-3-5-haiku-latest",
                DestinationAiProvider.Ollama => "llama3.2:3b",
                _ => string.Empty
            };
}

public enum DestinationAiProvider
{
    None,
    OpenAI,
    Grok,
    Claude,
    Ollama
}
