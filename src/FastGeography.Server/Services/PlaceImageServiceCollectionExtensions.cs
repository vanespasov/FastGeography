namespace FastGeography.Server.Services;

using FastGeography.Server.Options;

using Microsoft.Extensions.Options;

public static class PlaceImageServiceCollectionExtensions
{
    public static IServiceCollection AddPlaceImageServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PlaceImageOptions>(
            configuration.GetSection(PlaceImageOptions.Section));

        services.AddHttpClient("wikimedia", (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<PlaceImageOptions>>().Value;
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", opts.UserAgent);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddScoped<IPlaceImageService, PlaceImageService>();
        return services;
    }
}
