namespace FastGeography.IntegrationTests;

using FastGeography.IntegrationTests.Support;
using FastGeography.Server.Data;
using FastGeography.Server.Data.Seed;
using FastGeography.Server.Options;
using FastGeography.Server.Services;
using FastGeography.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>
/// Integration tests for <see cref="ToponymSeeder"/> and catalog-hit behaviour
/// after the well-known toponym data has been seeded.
/// </summary>
public sealed class ToponymSeederTests : IAsyncLifetime
{
    private readonly ServiceProvider _provider;
    private readonly SpyGeocodingService _spy;

    public ToponymSeederTests()
    {
        _spy = new SpyGeocodingService();
        var dbName = $"SeederTests-{Guid.NewGuid()}";

        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.Configure<GeocodingOptions>(o => o.Provider = "TestSpy");
        services.AddKeyedSingleton<IGeocodingService>("active", (_, _) => _spy);
        services.AddSingleton<IGeocodingService, CatalogGeocodingService>();
        services.AddLogging();
        _provider = services.BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
        await ToponymSeeder.SeedAsync(db);
    }

    public Task DisposeAsync()
    {
        _provider.Dispose();
        return Task.CompletedTask;
    }

    private IGeocodingService Catalog => _provider.GetRequiredService<IGeocodingService>();

    // ── Catalog hits after seeding ──────────────────────────────────────

    [Fact]
    public async Task AfterSeeding_LondonEnglish_IsCatalogHitWithoutCallingInner()
    {
        _spy.Reset(invalidByDefault: true);

        var result = await Catalog.ValidateAsync("London", LocationType.City, GameLanguage.En);

        Assert.Equal(ScoringRules.ValidPoints, result.Points);
        Assert.Equal(0, _spy.Calls);
    }

    [Fact]
    public async Task AfterSeeding_LondonMacedonian_IsCatalogHitWithoutCallingInner()
    {
        _spy.Reset(invalidByDefault: true);

        var result = await Catalog.ValidateAsync("Лондон", LocationType.City, GameLanguage.Mk);

        Assert.Equal(ScoringRules.ValidPoints, result.Points);
        Assert.Equal(0, _spy.Calls);
    }

    [Fact]
    public async Task AfterSeeding_BothLondonVariants_ReturnSameCoordinates()
    {
        var enResult = await Catalog.ValidateAsync("London", LocationType.City, GameLanguage.En);
        var mkResult = await Catalog.ValidateAsync("Лондон", LocationType.City, GameLanguage.Mk);

        Assert.NotNull(enResult.Coordinates);
        Assert.NotNull(mkResult.Coordinates);
        Assert.Equal(enResult.Coordinates, mkResult.Coordinates);
    }

    [Fact]
    public async Task AfterSeeding_VardarRiverMacedonian_IsCatalogHit()
    {
        _spy.Reset(invalidByDefault: true);

        var result = await Catalog.ValidateAsync("Вардар", LocationType.River, GameLanguage.Mk);

        Assert.Equal(ScoringRules.ValidPoints, result.Points);
        Assert.Equal(0, _spy.Calls);
    }

    [Fact]
    public async Task AfterSeeding_VardarRiverEnglish_IsCatalogHit()
    {
        _spy.Reset(invalidByDefault: true);

        var result = await Catalog.ValidateAsync("Vardar", LocationType.River, GameLanguage.En);

        Assert.Equal(ScoringRules.ValidPoints, result.Points);
        Assert.Equal(0, _spy.Calls);
    }

    // ── Cache miss for unseeded names ───────────────────────────────────

    [Fact]
    public async Task AfterSeeding_GarbageName_StillMissesCatalogAndCallsInner()
    {
        _spy.Reset(points: ScoringRules.InvalidPoints, coordinates: null);

        var result = await Catalog.ValidateAsync("XyzGarbage999", LocationType.City, GameLanguage.En);

        Assert.Equal(ScoringRules.InvalidPoints, result.Points);
        Assert.Equal(1, _spy.Calls);
    }

    // ── Idempotency ─────────────────────────────────────────────────────

    [Fact]
    public async Task RunSeederTwice_DoesNotCreateDuplicates()
    {
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var countBefore = await db.Toponyms.CountAsync();
        await ToponymSeeder.SeedAsync(db);
        var countAfter = await db.Toponyms.CountAsync();

        Assert.Equal(countBefore, countAfter);
    }

    // ── Provider tag ────────────────────────────────────────────────────

    [Fact]
    public async Task SeededToponyms_HaveProviderSeed()
    {
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var providers = await db.Toponyms
            .Select(t => t.Provider)
            .Distinct()
            .ToListAsync();

        Assert.Single(providers, "Seed");
    }

    // ── Total count sanity ──────────────────────────────────────────────

    [Fact]
    public async Task SeededToponyms_CountMatchesCatalogSize()
    {
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var dbCount = await db.Toponyms.CountAsync();
        Assert.Equal(WellKnownToponyms.All.Count, dbCount);
    }
}
