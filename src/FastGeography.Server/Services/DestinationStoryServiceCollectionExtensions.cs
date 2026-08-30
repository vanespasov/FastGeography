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

        // Backward compatibility: OpenAI:ApiKey / OpenAI:Model still work when DestinationAi is unset.
        services.PostConfigure<DestinationAiOptions>(opts =>
        {
            var legacyKey = configuration["OpenAI:ApiKey"];
            var legacyModel = configuration["OpenAI:Model"];

            if (opts.GetProvider() is DestinationAiProvider.None
                && !string.IsNullOrWhiteSpace(legacyKey))
            {
                opts.Provider = nameof(DestinationAiProvider.OpenAI);
                opts.ApiKey = legacyKey;
            }

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

        services.AddHttpClient("destination-ai");

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
}
