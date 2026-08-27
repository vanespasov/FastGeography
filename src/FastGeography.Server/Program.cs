using System.Threading.RateLimiting;

using FastGeography.Server.Options;
using FastGeography.Server.Services;

using Microsoft.AspNetCore.RateLimiting;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();

        builder.Services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();

        // --- Configuration ---
        builder.Services.Configure<BingMapsOptions>(
            builder.Configuration.GetSection(BingMapsOptions.Section));

        // --- Infrastructure ---
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<IGeocodingService, BingGeocodingService>();

        // --- Rate limiting: 60 geocode requests per minute per client ---
        builder.Services.AddRateLimiter(limiter =>
        {
            limiter.AddFixedWindowLimiter("geocode", o =>
            {
                o.PermitLimit = 60;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit = 0;
            });
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        if (!app.Environment.IsEnvironment("Testing"))
        {
            app.UseHttpsRedirection();
        }

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.UseRouting();
        app.UseRateLimiter();

        app.MapDefaultEndpoints();
        app.MapRazorPages();
        app.MapControllers();
        app.MapRazorComponents<FastGeography.Client.App>()
            .AddInteractiveWebAssemblyRenderMode();

        app.MapFallbackToFile("index.html");

        app.Run();
    }
}
