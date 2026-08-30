namespace FastGeography.Server.Services.Ai;

using FastGeography.Server.Options;

using Microsoft.Extensions.Options;

/// <summary>
/// Resolves the correct <see cref="IChatCompletionClient"/> for the configured provider.
/// </summary>
public static class ChatCompletionClientFactory
{
    public static IChatCompletionClient? Create(
        IServiceProvider sp,
        DestinationAiOptions options)
    {
        var provider = options.GetProvider();
        if (provider is DestinationAiProvider.None || !options.IsConfigured())
            return null;

        var model = options.GetEffectiveModel();
        var baseUrl = options.GetEffectiveBaseUrl();

        return provider switch
        {
            DestinationAiProvider.OpenAI or DestinationAiProvider.Grok or DestinationAiProvider.Ollama
                => CreateOpenAiCompatible(sp, baseUrl, model, options.ApiKey),

            DestinationAiProvider.Claude
                => CreateAnthropic(sp, baseUrl, model, options.ApiKey),

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
        http.Timeout = TimeSpan.FromSeconds(60);

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
        http.Timeout = TimeSpan.FromSeconds(60);

        return new AnthropicChatClient(
            http,
            model,
            apiKey,
            sp.GetRequiredService<ILogger<AnthropicChatClient>>());
    }
}
