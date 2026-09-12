namespace FastGeography.IntegrationTests;

using System.Net;
using System.Net.Http.Json;

using FastGeography.IntegrationTests.Support;
using FastGeography.Shared.Dtos;

public sealed class RankedSoloTests : IClassFixture<TestAppFixture>
{
    private readonly TestAppFixture _fixture;

    public RankedSoloTests(TestAppFixture fixture) => _fixture = fixture;

    private async Task<HttpClient> RegisteredClientAsync(string email, string name)
    {
        var client = _fixture.NewClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Pass123", name));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        return client;
    }

    [Fact]
    public async Task StartSolo_WhenAuthenticated_ReturnsRoundDetails()
    {
        var client = await RegisteredClientAsync("solo1@test.com", "SoloPlayer1");

        var resp = await client.PostAsync("/api/games/solo/start", null);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var data = await resp.Content.ReadFromJsonAsync<SoloStartResponse>();
        Assert.NotNull(data);
        Assert.NotEqual(Guid.Empty, data!.RoundId);
        Assert.InRange(data.Letter, 'A', 'Z');
        Assert.True(data.EndsAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task StartSolo_WhenNotAuthenticated_Returns401()
    {
        var client = _fixture.NewClient();
        var resp = await client.PostAsync("/api/games/solo/start", null);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task SubmitSolo_ValidAnswers_ReturnsTotalPoints()
    {
        var client = await RegisteredClientAsync("solo2@test.com", "SoloPlayer2");

        var startResp = await client.PostAsync("/api/games/solo/start", null);
        var start = await startResp.Content.ReadFromJsonAsync<SoloStartResponse>();

        var req = new SubmitAnswersRequest("London", "London", "London", "London", "London");
        var submitResp = await client.PostAsJsonAsync($"/api/games/solo/{start!.RoundId}/submit", req);

        Assert.Equal(HttpStatusCode.OK, submitResp.StatusCode);
        var result = await submitResp.Content.ReadFromJsonAsync<SoloSubmitResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result!.Badge);
        Assert.Equal(5, result.Details.Count);
    }

    [Fact]
    public async Task SubmitSolo_Twice_Returns409()
    {
        var client = await RegisteredClientAsync("solo3@test.com", "SoloPlayer3");

        var startResp = await client.PostAsync("/api/games/solo/start", null);
        var start = await startResp.Content.ReadFromJsonAsync<SoloStartResponse>();

        var req = new SubmitAnswersRequest(null, null, null, null, null);
        await client.PostAsJsonAsync($"/api/games/solo/{start!.RoundId}/submit", req);
        var resp2 = await client.PostAsJsonAsync($"/api/games/solo/{start.RoundId}/submit", req);

        Assert.Equal(HttpStatusCode.Conflict, resp2.StatusCode);
    }

    [Fact]
    public async Task SubmitSolo_UnknownRound_Returns404()
    {
        var client = await RegisteredClientAsync("solo4@test.com", "SoloPlayer4");

        var resp = await client.PostAsJsonAsync(
            $"/api/games/solo/{Guid.NewGuid()}/submit",
            new SubmitAnswersRequest(null, null, null, null, null));

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
