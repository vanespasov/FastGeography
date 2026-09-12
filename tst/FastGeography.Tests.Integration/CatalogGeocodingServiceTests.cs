namespace FastGeography.IntegrationTests;

using FastGeography.IntegrationTests.Support;
using FastGeography.Server.Data;
using FastGeography.Server.Data.Entities;
using FastGeography.Server.Options;
using FastGeography.Server.Services;
using FastGeography.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>
/// Verifies the <see cref="CatalogGeocodingService"/> lookup / insert / fallback logic
/// using an in-memory database and a controllable inner service spy — no real Bing calls.
/// </summary>
public sealed class CatalogGeocodingServiceTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly SpyGeocodingService _spy;

    public CatalogGeocodingServiceTests()
    {
        _spy = new SpyGeocodingService();
        var dbName = $"CatalogTests-{Guid.NewGuid()}";

        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.Configure<GeocodingOptions>(o => o.Provider = "TestSpy");
        services.AddKeyedSingleton<IGeocodingService>("active", (_, _) => _spy);
        services.AddSingleton<IGeocodingService, CatalogGeocodingService>();
        services.AddLogging();

        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
    }

    public void Dispose() => _provider.Dispose();

    private IGeocodingService Catalog =>
        _provider.GetRequiredService<IGeocodingService>();

    // ── Catalog hit ────────────────────────────────────────────────────────

    [Fact]
    public async Task CatalogHit_ReturnsValidPoints_WithoutCallingInner()
    {
        await SeedToponymAsync("london", "London", LocationType.City, 51.5074, -0.1278);

        _spy.Reset(invalidByDefault: false);

        var result = await Catalog.ValidateAsync("London", LocationType.City);

        Assert.Equal(ScoringRules.ValidPoints, result.Points);
        Assert.NotNull(result.Coordinates);
        Assert.Equal(0, _spy.Calls);
    }

    [Fact]
    public async Task CatalogHit_CoordinatesRoundTrip_MatchStoredValues()
    {
        await SeedToponymAsync("madrid", "Madrid", LocationType.City, 40.4168, -3.7038);

        var result = await Catalog.ValidateAsync("Madrid", LocationType.City);

        Assert.Equal("40.4168,-3.7038", result.Coordinates);
    }

    // ── Cache miss → Bing success → DB insert ─────────────────────────────

    [Fact]
    public async Task CacheMiss_ValidBingResult_InsertsToponymInDb()
    {
        _spy.Reset(points: ScoringRules.ValidPoints, coordinates: "48.8566,2.3522");

        await Catalog.ValidateAsync("Paris", LocationType.City);

        var toponym = await FindToponymAsync("paris", LocationType.City);
        Assert.NotNull(toponym);
        Assert.Equal("TestSpy", toponym.Provider);
        Assert.Equal(48.8566, toponym.Latitude);
        Assert.Equal(2.3522, toponym.Longitude);
    }

    [Fact]
    public async Task CacheMiss_ValidBingResult_ReturnsValidPoints()
    {
        _spy.Reset(points: ScoringRules.ValidPoints, coordinates: "52.5200,13.4050");

        var result = await Catalog.ValidateAsync("Berlin", LocationType.City);

        Assert.Equal(ScoringRules.ValidPoints, result.Points);
        Assert.Equal(1, _spy.Calls);
    }

    // ── Second call after Bing success ─────────────────────────────────────

    [Fact]
    public async Task SecondCall_HitsCatalog_NotBing()
    {
        _spy.Reset(points: ScoringRules.ValidPoints, coordinates: "41.9028,12.4964");

        await Catalog.ValidateAsync("Rome", LocationType.City);
        _spy.Calls = 0;

        var result = await Catalog.ValidateAsync("Rome", LocationType.City);

        Assert.Equal(0, _spy.Calls);
        Assert.Equal(ScoringRules.ValidPoints, result.Points);
    }

    // ── Cache miss → Bing failure → no DB insert ──────────────────────────

    [Fact]
    public async Task CacheMiss_InvalidBingResult_DoesNotInsertToponym()
    {
        _spy.Reset(points: ScoringRules.InvalidPoints, coordinates: null);

        await Catalog.ValidateAsync("NotACity", LocationType.City);

        var count = await CountToponymsAsync("notacity");
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task CacheMiss_InvalidBingResult_ReturnsInvalidPoints()
    {
        _spy.Reset(points: ScoringRules.InvalidPoints, coordinates: null);

        var result = await Catalog.ValidateAsync("NotACity", LocationType.City);

        Assert.Equal(ScoringRules.InvalidPoints, result.Points);
    }

    // ── Normalisation ──────────────────────────────────────────────────────

    [Fact]
    public async Task Normalization_LeadingAndTrailingWhitespace_Folds()
    {
        _spy.Reset(points: ScoringRules.ValidPoints, coordinates: "41.8781,-87.6298");

        await Catalog.ValidateAsync("  Chicago  ", LocationType.City);
        _spy.Calls = 0;

        var result = await Catalog.ValidateAsync("CHICAGO", LocationType.City);

        Assert.Equal(0, _spy.Calls);
        Assert.Equal(ScoringRules.ValidPoints, result.Points);
    }

    [Fact]
    public async Task Normalization_DifferentCasingOfSameName_HitsCatalog()
    {
        await SeedToponymAsync("sofia", "Sofia", LocationType.City, 42.6977, 23.3219);

        var result = await Catalog.ValidateAsync("SOFIA", LocationType.City);

        Assert.Equal(ScoringRules.ValidPoints, result.Points);
        Assert.Equal(0, _spy.Calls);
    }

    // ── Same name, different category ─────────────────────────────────────

    [Fact]
    public async Task SameNameDifferentCategory_AreIndependentCatalogEntries()
    {
        // Seed "Nile" as a River
        await SeedToponymAsync("nile", "Nile", LocationType.River, 30.0627, 31.2497);

        // Requesting "Nile" as Country should NOT hit the catalog
        _spy.Reset(points: ScoringRules.InvalidPoints, coordinates: null);

        await Catalog.ValidateAsync("Nile", LocationType.Country);

        Assert.Equal(1, _spy.Calls);
    }

    [Fact]
    public async Task SameNameSameCategory_OnlyInsertedOnce()
    {
        _spy.Reset(points: ScoringRules.ValidPoints, coordinates: "59.3293,18.0686");

        await Catalog.ValidateAsync("Stockholm", LocationType.City);
        await Catalog.ValidateAsync("Stockholm", LocationType.City);

        var count = await CountToponymsAsync("stockholm");
        Assert.Equal(1, count);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task SeedToponymAsync(
        string normalized, string display, LocationType category, double lat, double lon,
        string languageCode = "en")
    {
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Toponyms.Add(new Toponym
        {
            Id = Guid.NewGuid(),
            NormalizedName = normalized,
            DisplayName = display,
            Category = category,
            LanguageCode = languageCode,
            Latitude = lat,
            Longitude = lon,
            Provider = "Bing",
            VerifiedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private async Task<Toponym?> FindToponymAsync(string normalized, LocationType category,
        string languageCode = "en")
    {
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Toponyms
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.NormalizedName == normalized
                                      && t.Category == category
                                      && t.LanguageCode == languageCode);
    }

    private async Task<int> CountToponymsAsync(string normalized, string? languageCode = null)
    {
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Toponyms.CountAsync(t => t.NormalizedName == normalized
                                                 && (languageCode == null || t.LanguageCode == languageCode));
    }

    // ── Language isolation ────────────────────────────────────────────────

    [Fact]
    public async Task SameNameSameCategory_DifferentLanguage_AreIndependentCacheRows()
    {
        // "Paris" (EN) and its Macedonian equivalent under the same normalised key are
        // separate rows because LanguageCode is part of the unique index.
        await SeedToponymAsync("paris", "Paris", LocationType.City, 48.8566, 2.3522, "en");
        await SeedToponymAsync("париз", "Париз", LocationType.City, 48.8566, 2.3522, "mk");

        var enResult = await Catalog.ValidateAsync("Paris", LocationType.City, GameLanguage.En);
        var mkResult = await Catalog.ValidateAsync("Париз", LocationType.City, GameLanguage.Mk);

        Assert.Equal(ScoringRules.ValidPoints, enResult.Points);
        Assert.Equal(ScoringRules.ValidPoints, mkResult.Points);
        Assert.Equal(0, _spy.Calls);
    }

    [Fact]
    public async Task CatalogHit_EnglishSeed_DoesNotMatchMacedonianRequest()
    {
        await SeedToponymAsync("london", "London", LocationType.City, 51.5074, -0.1278, "en");
        _spy.Reset(invalidByDefault: true);

        // Asking for "london" in MK should miss the EN-only cache row
        var result = await Catalog.ValidateAsync("London", LocationType.City, GameLanguage.Mk);

        Assert.Equal(1, _spy.Calls);
        Assert.Equal(ScoringRules.InvalidPoints, result.Points);
    }

    [Fact]
    public async Task CacheMiss_MacedonianAnswer_PersistedWithMkLanguageCode()
    {
        _spy.Reset(points: ScoringRules.ValidPoints, coordinates: "41.9981,21.4254");

        await Catalog.ValidateAsync("Скопје", LocationType.City, GameLanguage.Mk);

        var toponym = await FindToponymAsync("скопје", LocationType.City, "mk");
        Assert.NotNull(toponym);
        Assert.Equal("mk", toponym.LanguageCode);
    }

    // ── Spy is now in Support/SpyGeocodingService.cs ───────────────────────
}
