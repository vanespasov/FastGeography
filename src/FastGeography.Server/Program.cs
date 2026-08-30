using System.Threading.RateLimiting;

using FastGeography.Server.Data;
using FastGeography.Server.Data.Seed;
using FastGeography.Server.Hubs;
using FastGeography.Server.Options;
using FastGeography.Server.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

        // --- Geocoding configuration ---
        builder.Services.Configure<GeocodingOptions>(
            builder.Configuration.GetSection(GeocodingOptions.Section));

        // --- HTTP clients for geocoding adapters ---
        builder.Services.AddHttpClient("nominatim", (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<GeocodingOptions>>().Value.Nominatim;
            client.BaseAddress = new Uri(opts.BaseUrl);
            // TryAddWithoutValidation: User-Agent comments with URLs/semicolons fail ParseAdd.
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", opts.UserAgent);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        builder.Services.AddHttpClient("geonames", (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<GeocodingOptions>>().Value.GeoNames;
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // --- Infrastructure ---
        builder.Services.AddMemoryCache();

        // Register all geocoding adapters under their own keys.
        builder.Services.AddKeyedSingleton<IGeocodingService, BingGeocodingService>("bing");
        builder.Services.AddKeyedSingleton<IGeocodingService, NominatimGeocodingService>("nominatim");
        builder.Services.AddKeyedSingleton<IGeocodingService, GeoNamesGeocodingService>("geonames");

        // "active" → whichever adapter is selected by Geocoding:Provider (resolved lazily
        //            so the factory reads the live options value).
        builder.Services.AddKeyedSingleton<IGeocodingService>("active", (sp, _) =>
        {
            var opts = sp.GetRequiredService<IOptions<GeocodingOptions>>().Value;
            var key = opts.Provider.ToLowerInvariant() switch
            {
                "geonames" => "geonames",
                "bing"     => "bing",
                _          => "nominatim"   // default: Nominatim
            };
            return sp.GetRequiredKeyedService<IGeocodingService>(key);
        });

        // Unkeyed → CatalogGeocodingService decorator: checks DB first, falls back to
        // "active" provider, and persists confirmed results for future lookups.
        builder.Services.AddSingleton<IGeocodingService, CatalogGeocodingService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddSingleton<IRoomService, RoomService>();

        // --- Destination AI story service (OpenAI, Grok, Claude, Ollama) ---
        builder.Services.AddDestinationStoryServices(builder.Configuration);

        // --- Rate limiting ---
        builder.Services.AddRateLimiter(limiter =>
        {
            // 60 geocode requests per minute per client
            limiter.AddFixedWindowLimiter("geocode", o =>
            {
                o.PermitLimit = 60;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit = 0;
            });
            // 20 story requests per minute per client
            limiter.AddFixedWindowLimiter("stories", o =>
            {
                o.PermitLimit = 20;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit = 0;
            });
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        var app = builder.Build();

        // --- DB initialisation ---
        // • PostgreSQL  → MigrateAsync: applies any pending migrations so existing
        //                 databases are brought up to date without data loss.
        // • InMemory dev (no connection string) → EnsureCreated: in-memory stores
        //   don't support migrations; schema is recreated fresh on each run anyway.
        // • Testing environment → skipped entirely; each test fixture manages its
        //   own in-memory database via EnsureCreated.
        if (!app.Environment.IsEnvironment("Testing"))
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                if (usePostgres)
                    await db.Database.MigrateAsync();
                else
                    await db.Database.EnsureCreatedAsync();

                await ToponymSeeder.SeedAsync(db);
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
