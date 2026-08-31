namespace FastGeography.Server.Services;

using FastGeography.Server.Options;
using FastGeography.Server.Services.Ai;

using Microsoft.Extensions.Options;

public static class DestinationStoryServiceCollectionExtensions
{
    public static IServiceCollection AddDestinationStoryServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DestinationAiOptions>(
            configuration.GetSection(DestinationAiOptions.Section));

        services.PostConfigure<DestinationAiOptions>(opts =>
        {
            var legacyKey = configuration["OpenAI:ApiKey"];
            var envKey = configuration["OPENAI_API_KEY"];
            var legacyModel = configuration["OpenAI:Model"];

            if (string.IsNullOrWhiteSpace(opts.ApiKey))
                opts.ApiKey = FirstNonEmpty(legacyKey, envKey);

            if (string.IsNullOrWhiteSpace(opts.Model) && !string.IsNullOrWhiteSpace(legacyModel))
                opts.Model = legacyModel;

            if (string.IsNullOrWhiteSpace(opts.ApiKey))
            {
                opts.ApiKey = opts.GetProvider() switch
                {
                    DestinationAiProvider.Claude => configuration["ANTHROPIC_API_KEY"] ?? string.Empty,
                    DestinationAiProvider.Grok => configuration["GROK_API_KEY"] ?? string.Empty,
                    _ => opts.ApiKey
                };
            }
        });

        // Aspire AddServiceDefaults() attaches a 10s StandardResilienceHandler to every
        // HttpClient. Chat completions (especially local Ollama) often exceed that.
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is experimental
        services.AddHttpClient("destination-ai", client =>
            client.Timeout = TimeSpan.FromMinutes(2))
            .RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

        services.AddSingleton<IDestinationStoryService>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<DestinationAiOptions>>().Value;
            if (!opts.IsConfigured())
                return new NullDestinationStoryService();

            var chat = ChatCompletionClientFactory.Create(sp, opts);
            if (chat is null)
                return new NullDestinationStoryService();

            return new DestinationStoryService(
                chat,
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<DestinationStoryService>>());
        });

        return services;
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
}
