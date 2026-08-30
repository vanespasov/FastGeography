namespace FastGeography.IntegrationTests;

using System.Net;
using System.Net.Http.Json;

using FastGeography.IntegrationTests.Support;
using FastGeography.Server.Data;
using FastGeography.Server.Data.Entities;
using FastGeography.Shared;
using FastGeography.Shared.Dtos;

using Microsoft.Extensions.DependencyInjection;

public sealed class DestinationStoriesTests : IClassFixture<TestAppFixture>
{
    private readonly TestAppFixture _fixture;

    public DestinationStoriesTests(TestAppFixture fixture) => _fixture = fixture;

    /// <summary>Seeds a Toponym row so the controller considers the place verified.</summary>
    private async Task SeedToponymAsync(string display, LocationType type, string langCode)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var normalized = display.Trim().ToLowerInvariant();
        if (!db.Toponyms.Any(t => t.NormalizedName == normalized && t.Category == type && t.LanguageCode == langCode))
        {
            db.Toponyms.Add(new Toponym
            {
                Id = Guid.NewGuid(),
                NormalizedName = normalized,
                DisplayName = display,
                Category = type,
                LanguageCode = langCode,
                Latitude = 41.99,
                Longitude = 21.43,
                Provider = "Test",
                VerifiedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task PostStories_ForVerifiedPlace_ReturnsStory()
    {
        await SeedToponymAsync("Skopje", LocationType.City, "en");

        var client = _fixture.NewClient();
        var request = new DestinationStoriesRequest(new List<StoryRequest>
        {
            new("Skopje", LocationType.City, "41.99,21.43", "en")
        });

        var resp = await client.PostAsJsonAsync("/api/destination-stories", request);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var data = await resp.Content.ReadFromJsonAsync<DestinationStoriesResponse>();
        Assert.NotNull(data);
        Assert.Single(data!.Stories);
        Assert.Equal("Skopje", data.Stories[0].Name);
        Assert.False(string.IsNullOrWhiteSpace(data.Stories[0].Story));
    }

    [Fact]
    public async Task PostStories_ForUnverifiedPlace_ReturnsEmpty()
    {
        var client = _fixture.NewClient();
        var request = new DestinationStoriesRequest(new List<StoryRequest>
        {
            new("NonExistentPlace999", LocationType.City, null, "en")
        });

        var resp = await client.PostAsJsonAsync("/api/destination-stories", request);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var data = await resp.Content.ReadFromJsonAsync<DestinationStoriesResponse>();
        Assert.NotNull(data);
        Assert.Empty(data!.Stories);
    }

    [Fact]
    public async Task PostStories_EmptyList_ReturnsBadRequest()
    {
        var client = _fixture.NewClient();
        var request = new DestinationStoriesRequest(new List<StoryRequest>());

        var resp = await client.PostAsJsonAsync("/api/destination-stories", request);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
