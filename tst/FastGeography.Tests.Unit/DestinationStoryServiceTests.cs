namespace FastGeography.Tests.Unit;

using FastGeography.Server.Data;
using FastGeography.Server.Data.Entities;
using FastGeography.Server.Services;
using FastGeography.Server.Services.Ai;
using FastGeography.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class DestinationStoryServiceTests
{
    [Fact]
    public async Task FullPool_ReturnsExistingStory_WithoutCallingAi()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedStoriesAsync(dbName, count: DestinationStoryService.TargetPoolSize);

        var chat = new StubChatClient("should-not-be-used");
        var sut = CreateSut(dbName, chat);

        var story = await sut.GetStoryAsync("Skopje", LocationType.City, "41.99,21.43", GameLanguage.En);

        Assert.NotNull(story);
        Assert.StartsWith("Cached story", story, StringComparison.Ordinal);
        Assert.Equal(0, chat.Calls);
    }

    [Fact]
    public async Task ShortPool_GeneratesAndPersistsNewStory()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedStoriesAsync(dbName, count: 2);

        var chat = new StubChatClient("A brand new witty fact about Skopje.");
        var sut = CreateSut(dbName, chat);

        var story = await sut.GetStoryAsync("Skopje", LocationType.City, "41.99,21.43", GameLanguage.En);

        Assert.Equal("A brand new witty fact about Skopje.", story);
        Assert.Equal(1, chat.Calls);
        Assert.Contains("Do not repeat these openings or facts", chat.LastUserPrompt, StringComparison.Ordinal);

        await using var db = CreateDb(dbName);
        Assert.Equal(3, await db.ToponymStories.CountAsync());
    }

    [Fact]
    public async Task AiFails_WithExistingPool_ReturnsExistingStory()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedStoriesAsync(dbName, count: 2);

        var chat = new StubChatClient(null);
        var sut = CreateSut(dbName, chat);

        var story = await sut.GetStoryAsync("Skopje", LocationType.City, null, GameLanguage.En);

        Assert.NotNull(story);
        Assert.StartsWith("Cached story", story, StringComparison.Ordinal);
        Assert.Equal(1, chat.Calls);

        await using var db = CreateDb(dbName);
        Assert.Equal(2, await db.ToponymStories.CountAsync());
    }

    [Fact]
    public async Task AiFails_EmptyPool_ReturnsNull()
    {
        var dbName = Guid.NewGuid().ToString();
        var chat = new StubChatClient(null);
        var sut = CreateSut(dbName, chat);

        var story = await sut.GetStoryAsync("Skopje", LocationType.City, null, GameLanguage.En);

        Assert.Null(story);
        Assert.Equal(1, chat.Calls);
    }

    [Fact]
    public async Task PersistsStory_WithoutMatchingLanguageToponym()
    {
        var dbName = Guid.NewGuid().ToString();
        var chat = new StubChatClient("Приказна на македонски.");
        var sut = CreateSut(dbName, chat);

        var story = await sut.GetStoryAsync("Skopje", LocationType.City, null, GameLanguage.Mk);

        Assert.Equal("Приказна на македонски.", story);

        await using var db = CreateDb(dbName);
        var saved = Assert.Single(db.ToponymStories);
        Assert.Equal("skopje", saved.NormalizedName);
        Assert.Equal("mk", saved.LanguageCode);
        Assert.Equal("Приказна на македонски.", saved.Body);
        Assert.False(string.IsNullOrWhiteSpace(saved.Angle));
    }

    [Fact]
    public void PickUnusedAngle_SkipsAnglesAlreadyInPool()
    {
        var pool = DestinationStoryPrompt.AllAngles
            .Take(5)
            .Select(a => new ToponymStory { Angle = a.ToKey(), Body = a.ToKey() })
            .ToList();

        var unused = DestinationStoryService.PickUnusedAngle(pool);

        Assert.Equal(StoryAngle.GeographyShape, unused);
    }

    private static DestinationStoryService CreateSut(string dbName, IChatCompletionClient chat)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
        var provider = services.BuildServiceProvider();

        return new DestinationStoryService(
            chat,
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<DestinationStoryService>.Instance);
    }

    private static ApplicationDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task SeedStoriesAsync(string dbName, int count)
    {
        await using var db = CreateDb(dbName);
        for (var i = 0; i < count; i++)
        {
            var angle = DestinationStoryPrompt.AllAngles[i];
            db.ToponymStories.Add(new ToponymStory
            {
                Id = Guid.NewGuid(),
                NormalizedName = "skopje",
                Category = LocationType.City,
                LanguageCode = "en",
                Body = $"Cached story {i} about Skopje.",
                Angle = angle.ToKey(),
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }

    private sealed class StubChatClient : IChatCompletionClient
    {
        private readonly string? _result;

        public int Calls { get; private set; }
        public string LastUserPrompt { get; private set; } = string.Empty;

        public StubChatClient(string? result) => _result = result;

        public Task<string?> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        {
            Calls++;
            LastUserPrompt = userPrompt;
            return Task.FromResult(_result);
        }
    }
}
