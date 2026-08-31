namespace FastGeography.Server.Options;

/// <summary>
/// Configuration for destination story AI. Bind from the "DestinationAi" config section.
/// Secrets via user-secrets or environment variables (DestinationAi__ApiKey / OPENAI_API_KEY).
/// </summary>
public sealed class DestinationAiOptions
{
    public const string Section = "DestinationAi";

    /// <summary>
    /// Active provider. <c>Auto</c> (default) tries OpenAI first, then Ollama.
    /// Explicit values: None, Auto, OpenAI, Grok, Claude, Ollama.
    /// </summary>
    public string Provider { get; set; } = "Auto";

    /// <summary>Default model when a provider-specific model is not set.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>API key for OpenAI, Grok, or Claude (depending on provider).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Optional global base URL override. Prefer <see cref="OllamaBaseUrl"/> for Ollama.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Ollama OpenAI-compatible base URL (e.g. http://localhost:11434/v1).</summary>
    public string OllamaBaseUrl { get; set; } = string.Empty;

    /// <summary>Ollama model tag. Defaults to llama3.2:3b.</summary>
    public string OllamaModel { get; set; } = "llama3.2:3b";

    public DestinationAiProvider GetProvider() =>
        Enum.TryParse<DestinationAiProvider>(Provider, ignoreCase: true, out var p)
            ? p
            : DestinationAiProvider.Auto;

    /// <summary>
    /// Provider attempt order. Auto → OpenAI then Ollama. A named provider is a single-item chain.
    /// </summary>
    public IReadOnlyList<DestinationAiProvider> GetFallbackChain()
    {
        var provider = GetProvider();
        return provider switch
        {
            DestinationAiProvider.Auto => [DestinationAiProvider.OpenAI, DestinationAiProvider.Ollama],
            DestinationAiProvider.None => [],
            _ => [provider]
        };
    }

    public bool IsConfigured() =>
        GetFallbackChain().Any(IsProviderConfigured);

    public bool IsProviderConfigured(DestinationAiProvider provider) =>
        provider switch
        {
            DestinationAiProvider.None or DestinationAiProvider.Auto => false,
            DestinationAiProvider.Ollama => !string.IsNullOrWhiteSpace(GetBaseUrl(provider)),
            _ => !string.IsNullOrWhiteSpace(GetApiKey(provider))
        };

    public string GetApiKey(DestinationAiProvider provider) =>
        provider is DestinationAiProvider.Ollama ? string.Empty : ApiKey;

    public string GetBaseUrl(DestinationAiProvider provider)
    {
        if (provider is DestinationAiProvider.Ollama && !string.IsNullOrWhiteSpace(OllamaBaseUrl))
            return OllamaBaseUrl.TrimEnd('/');

        if (!string.IsNullOrWhiteSpace(BaseUrl) && provider is not DestinationAiProvider.Ollama)
            return BaseUrl.TrimEnd('/');

        if (!string.IsNullOrWhiteSpace(BaseUrl) && provider is DestinationAiProvider.Ollama
            && string.IsNullOrWhiteSpace(OllamaBaseUrl))
            return BaseUrl.TrimEnd('/');

        return provider switch
        {
            DestinationAiProvider.OpenAI => "https://api.openai.com/v1",
            DestinationAiProvider.Grok => "https://api.x.ai/v1",
            DestinationAiProvider.Claude => "https://api.anthropic.com",
            DestinationAiProvider.Ollama => "http://localhost:11434/v1",
            _ => string.Empty
        };
    }

    public string GetModel(DestinationAiProvider provider) =>
        provider switch
        {
            DestinationAiProvider.Ollama => FirstNonEmpty(OllamaModel, Model, "llama3.2:3b"),
            DestinationAiProvider.OpenAI => FirstNonEmpty(Model, "gpt-4o-mini"),
            DestinationAiProvider.Grok => FirstNonEmpty(Model, "grok-2-1212"),
            DestinationAiProvider.Claude => FirstNonEmpty(Model, "claude-3-5-haiku-latest"),
            _ => Model
        };

    // Kept for existing tests that call the old names.
    public string GetEffectiveBaseUrl() => GetBaseUrl(GetProvider() is DestinationAiProvider.Auto
        ? DestinationAiProvider.OpenAI
        : GetProvider());

    public string GetEffectiveModel() => GetModel(GetProvider() is DestinationAiProvider.Auto
        ? DestinationAiProvider.OpenAI
        : GetProvider());

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
}

public enum DestinationAiProvider
{
    None,
    Auto,
    OpenAI,
    Grok,
    Claude,
    Ollama
}
