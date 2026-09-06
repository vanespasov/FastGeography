namespace FastGeography.Tests.Unit;

using System.Net;
using System.Text;

using FastGeography.Server.Data;
using FastGeography.Server.Data.Entities;
using FastGeography.Server.Options;
using FastGeography.Server.Services;
using FastGeography.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

public sealed class PlaceImageServiceTests
{
    [Fact]
    public async Task WikipediaHit_ReturnsThumbnail()
    {
        var json = """{"thumbnail":{"source":"https://upload.wikimedia.org/skopje.jpg"}}""";
        var client = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var result = await PlaceImageService.TryWikipediaSummaryAsync(
            client, "en", "Skopje", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("https://upload.wikimedia.org/skopje.jpg", result!.ImageUrl);
        Assert.Equal("Wikipedia", result.Attribution);
    }

    [Fact]
    public async Task WikipediaMiss_FallsThroughToCommons()
    {
        var commonsJson = """
            {"query":{"pages":{"1":{"thumbnail":{"source":"https://upload.wikimedia.org/commons.jpg"}}}}}
            """;
        var call = 0;
        var client = CreateHttpClient(req =>
        {
            call++;
            if (req.RequestUri!.Host.Contains("wikipedia"))
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(commonsJson, Encoding.UTF8, "application/json")
            };
        });

        var wiki = await PlaceImageService.TryWikipediaSummaryAsync(client, "en", "Obscure", CancellationToken.None);
        Assert.Null(wiki);

        var commons = await PlaceImageService.TryCommonsGeosearchAsync(client, 41.99, 21.43, CancellationToken.None);
        Assert.NotNull(commons);
        Assert.Equal("https://upload.wikimedia.org/commons.jpg", commons!.ImageUrl);
        Assert.Equal("Wikimedia Commons", commons.Attribution);
        Assert.True(call >= 1);
    }

    [Fact]
    public async Task CachedImage_ReturnsWithoutHttpCall()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedToponymAsync(dbName, imageUrl: "https://example.com/cached.jpg", fetchedAt: DateTime.UtcNow);

        var handler = new ThrowingHandler();
        var sut = CreateSut(dbName, handler);

        var result = await sut.GetImageAsync(
            "skopje", LocationType.City, "Skopje", 41.99, 21.43, GameLanguage.En);

        Assert.NotNull(result);
        Assert.Equal("https://example.com/cached.jpg", result!.ImageUrl);
    }

    [Fact]
    public async Task CachedMiss_DoesNotRefetchWithinRetryWindow()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedToponymAsync(dbName, imageUrl: null, fetchedAt: DateTime.UtcNow);

        var handler = new ThrowingHandler();
        var sut = CreateSut(dbName, handler);

        var result = await sut.GetImageAsync(
            "skopje", LocationType.City, "Skopje", 41.99, 21.43, GameLanguage.En);

        Assert.Null(result);
    }

    private static PlaceImageService CreateSut(string dbName, HttpMessageHandler handler)
    {
        var db = CreateDb(dbName);
        var factory = new StubHttpClientFactory(handler);
        var options = Options.Create(new PlaceImageOptions { MissRetryDays = 30 });
        return new PlaceImageService(db, factory, options, NullLogger<PlaceImageService>.Instance);
    }

    private static HttpClient CreateHttpClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        return new HttpClient(new DelegateHandler(responder))
        {
            BaseAddress = new Uri("https://example.test/")
        };
    }

    private static ApplicationDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task SeedToponymAsync(string dbName, string? imageUrl, DateTime fetchedAt)
    {
        await using var db = CreateDb(dbName);
        db.Toponyms.Add(new Toponym
        {
            Id = Guid.NewGuid(),
            NormalizedName = "skopje",
            DisplayName = "Skopje",
            Category = LocationType.City,
            LanguageCode = "en",
            Latitude = 41.99,
            Longitude = 21.43,
            Provider = "Test",
            VerifiedAtUtc = DateTime.UtcNow,
            ImageUrl = imageUrl,
            ImageAttribution = imageUrl is null ? null : "Wikipedia",
            ImageFetchedAtUtc = fetchedAt
        });
        await db.SaveChangesAsync();
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false) { BaseAddress = new Uri("https://example.test/") };
    }

    private sealed class DelegateHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("HTTP should not be called when cache is warm.");
    }
}
