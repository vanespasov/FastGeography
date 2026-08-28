using System.Threading.RateLimiting;

using FastGeography.Server.Data;
using FastGeography.Server.Hubs;
using FastGeography.Server.Options;
using FastGeography.Server.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        // --- EF Core: PostgreSQL when connection string present, InMemory otherwise ---
        // When running via Aspire the connection string is injected automatically.
        // For local development without Aspire (no Docker), InMemory is used so the
        // server still boots — auth/ranked features work but data is not persisted.
        var connStr = builder.Configuration.GetConnectionString("fastgeography-db");
        var usePostgres = !string.IsNullOrWhiteSpace(connStr);

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (usePostgres)
                options.UseNpgsql(connStr);
            else
                options.UseInMemoryDatabase("FastGeographyDev");
        });

        // --- Identity ---
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireDigit = false;
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders()
        .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api") ||
                    ctx.Request.Path.StartsWithSegments("/hubs"))
                {
                    ctx.Response.StatusCode = 401;
                    return Task.CompletedTask;
                }
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api") ||
                    ctx.Request.Path.StartsWithSegments("/hubs"))
                {
                    ctx.Response.StatusCode = 403;
                    return Task.CompletedTask;
                }
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            };
        });

        builder.Services.AddAuthorization();

        // --- SignalR ---
        builder.Services.AddSignalR();

        // --- MVC / Razor ---
        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();

        // --- Configuration ---
        builder.Services.Configure<BingMapsOptions>(
            builder.Configuration.GetSection(BingMapsOptions.Section));

        // --- Infrastructure ---
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<IGeocodingService, BingGeocodingService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddSingleton<IRoomService, RoomService>();

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

        // --- DB: create schema on first run ---
        // Skipped in Testing (uses InMemory per-test) and when startup DB init fails.
        if (!app.Environment.IsEnvironment("Testing"))
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await db.Database.EnsureCreatedAsync();
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogWarning(ex,
                    "Database initialisation skipped — " +
                    "auth and ranked features require a running database. " +
                    "Run via Aspire AppHost to start PostgreSQL automatically.");
            }
        }

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
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        app.MapDefaultEndpoints();
        app.MapRazorPages();
        app.MapControllers();
        app.MapHub<GameHub>("/hubs/game");

        // Hosted Blazor WASM: serve index.html so blazor.webassembly.js boots the client.
        app.MapFallbackToFile("index.html");

        await app.RunAsync();
    }
}
