namespace FastGeography.Server.Services.Ai;

using FastGeography.Server.Options;

/// <summary>
/// Resolves chat clients for the configured provider chain (OpenAI first, then Ollama when Auto).
/// </summary>
public static class ChatCompletionClientFactory
{
    public static IChatCompletionClient? Create(
        IServiceProvider sp,
        DestinationAiOptions options)
    {
        var clients = new List<(string Name, IChatCompletionClient Client)>();

        foreach (var provider in options.GetFallbackChain())
        {
            if (!options.IsProviderConfigured(provider))
                continue;

            var client = CreateForProvider(sp, options, provider);
            if (client is not null)
                clients.Add((provider.ToString(), client));
        }

        if (clients.Count == 0)
            return null;

        if (clients.Count == 1)
            return clients[0].Client;

        return new FallbackChatClient(
            clients,
            sp.GetRequiredService<ILogger<FallbackChatClient>>());
    }

    private static IChatCompletionClient? CreateForProvider(
        IServiceProvider sp,
        DestinationAiOptions options,
        DestinationAiProvider provider)
    {
        var model = options.GetModel(provider);
        var baseUrl = options.GetBaseUrl(provider);
        var apiKey = options.GetApiKey(provider);

        return provider switch
        {
            DestinationAiProvider.OpenAI or DestinationAiProvider.Grok or DestinationAiProvider.Ollama
                => CreateOpenAiCompatible(sp, baseUrl, model, apiKey),

            DestinationAiProvider.Claude
                => CreateAnthropic(sp, baseUrl, model, apiKey),

            _ => null
        };
    }

    private static IChatCompletionClient CreateOpenAiCompatible(
        IServiceProvider sp,
        string baseUrl,
        string model,
        string apiKey)
    {
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var http = factory.CreateClient("destination-ai");
        http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

        return new OpenAiCompatibleChatClient(
            http,
            model,
            string.IsNullOrWhiteSpace(apiKey) ? null : apiKey,
            sp.GetRequiredService<ILogger<OpenAiCompatibleChatClient>>());
    }

    private static IChatCompletionClient CreateAnthropic(
        IServiceProvider sp,
        string baseUrl,
        string model,
        string apiKey)
    {
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var http = factory.CreateClient("destination-ai");
        http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

        return new AnthropicChatClient(
            http,
            model,
            apiKey,
            sp.GetRequiredService<ILogger<AnthropicChatClient>>());
    }
}
