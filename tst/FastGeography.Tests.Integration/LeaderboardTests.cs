namespace FastGeography.IntegrationTests;

using System.Net;
using System.Net.Http.Json;

using FastGeography.IntegrationTests.Support;
using FastGeography.Shared.Dtos;

public sealed class LeaderboardTests : IClassFixture<TestAppFixture>
{
    private readonly TestAppFixture _fixture;

    public LeaderboardTests(TestAppFixture fixture) => _fixture = fixture;

    private async Task<HttpClient> RegisterAndPlayAsync(string email, string name)
    {
        var client = _fixture.NewClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Pass123", name));

        var startResp = await client.PostAsync("/api/games/solo/start", null);
        var start = await startResp.Content.ReadFromJsonAsync<SoloStartResponse>();
        // London is a known city in FakeGeocodingService
        await client.PostAsJsonAsync($"/api/games/solo/{start!.RoundId}/submit",
            new SubmitAnswersRequest("London", "London", "London", "London", "London"));

        return client;
    }

    [Fact]
    public async Task GetLeaderboard_AllTime_Returns200()
    {
        var client = _fixture.NewClient();
        var resp = await client.GetAsync("/api/leaderboard");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GetLeaderboard_AfterPlaying_PlayerAppears()
    {
        await RegisterAndPlayAsync($"lb1-{Guid.NewGuid():N}@test.com", "BoardPlayer1");

        var client = _fixture.NewClient();
        var entries = await client.GetFromJsonAsync<List<LeaderboardEntry>>("/api/leaderboard");

        Assert.NotNull(entries);
        Assert.NotEmpty(entries!);
    }

    [Fact]
    public async Task GetMyStats_WhenAuthenticated_ReturnsStats()
    {
        var email = $"mystats-{Guid.NewGuid():N}@test.com";
        var client = await RegisterAndPlayAsync(email, "StatsPlayer");

        var stats = await client.GetFromJsonAsync<PlayerStats>("/api/leaderboard/me");

        Assert.NotNull(stats);
        Assert.True(stats!.GamesPlayed >= 1);
        Assert.True(stats.CareerPoints != 0);
    }

    [Fact]
    public async Task GetMyStats_WhenNotAuthenticated_Returns401()
    {
        var client = _fixture.NewClient();
        var resp = await client.GetAsync("/api/leaderboard/me");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
