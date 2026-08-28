namespace FastGeography.IntegrationTests;

using System.Net;
using System.Net.Http.Json;

using FastGeography.IntegrationTests.Support;
using FastGeography.Shared.Dtos;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

/// <summary>
/// Tests for the <c>GameHub</c> SignalR hub.
/// Uses <see cref="Microsoft.AspNetCore.TestHost.TestServer"/> so no real network socket is opened.
/// Auth cookies are shared between the REST calls and the SignalR negotiate/transport via a
/// <see cref="SharedCookieHandler"/> that wraps the TestServer's inner handler.
/// </summary>
public sealed class GameHubTests : IClassFixture<TestAppFixture>
{
    private readonly TestAppFixture _fixture;

    public GameHubTests(TestAppFixture fixture) => _fixture = fixture;

    private async Task<(HttpClient RestClient, HubConnection Hub)> ConnectAsync(
        string email, string displayName)
    {
        // One handler instance shared between the REST HttpClient and the hub, so the
        // auth cookie set during registration is automatically forwarded to SignalR negotiate.
        var sharedHandler = new SharedCookieHandler(_fixture.Factory.Server.CreateHandler());
        var baseAddress = new Uri("http://localhost");

        var restClient = new HttpClient(sharedHandler, disposeHandler: false)
        {
            BaseAddress = baseAddress
        };

        var reg = await restClient.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Pass123", displayName));
        Assert.Equal(HttpStatusCode.OK, reg.StatusCode);

        var hub = new HubConnectionBuilder()
            .WithUrl(new Uri(baseAddress, "/hubs/game").ToString(), opts =>
            {
                // Long-polling routes all traffic through HttpMessageHandlerFactory,
                // allowing the same cookie jar to be used for negotiate + poll requests.
                opts.Transports = HttpTransportType.LongPolling;
                opts.HttpMessageHandlerFactory = _ => sharedHandler;
            })
            .Build();

        await hub.StartAsync();
        return (restClient, hub);
    }

    [Fact]
    public async Task CreateRoom_ViaRestApi_ReturnsRoomCode()
    {
        var (restClient, hub) = await ConnectAsync(
            $"hub1-{Guid.NewGuid():N}@test.com", "HubPlayer1");

        try
        {
            var resp = await restClient.PostAsync("/api/rooms", null);
            resp.EnsureSuccessStatusCode();
            var room = await resp.Content.ReadFromJsonAsync<CreateRoomResponse>();

            Assert.NotNull(room);
            Assert.Equal(6, room!.RoomCode.Length);

            // Verify the room exists
            var existsResp = await restClient.GetAsync($"/api/rooms/{room.RoomCode}/exists");
            Assert.Equal(HttpStatusCode.OK, existsResp.StatusCode);
        }
        finally
        {
            await hub.DisposeAsync();
            restClient.Dispose();
        }
    }

    [Fact]
    public async Task JoinRoom_WithValidCode_ReceivesRoomJoined()
    {
        var (restClient, hub) = await ConnectAsync(
            $"hub2-{Guid.NewGuid():N}@test.com", "HubPlayer2");

        try
        {
            var resp = await restClient.PostAsync("/api/rooms", null);
            resp.EnsureSuccessStatusCode();
            var room = await resp.Content.ReadFromJsonAsync<CreateRoomResponse>();

            RoomStateDto? state = null;
            hub.On<RoomStateDto>("RoomJoined", s => state = s);

            await hub.SendAsync("JoinRoom", room!.RoomCode);

            await WaitUntil(() => state is not null);

            Assert.NotNull(state);
            Assert.Equal(room.RoomCode, state!.RoomCode);
            Assert.NotEmpty(state.Players);
        }
        finally
        {
            await hub.DisposeAsync();
            restClient.Dispose();
        }
    }

    [Fact]
    public async Task StartRound_AsHost_BroadcastsRoundStarted()
    {
        var (restClient, hub) = await ConnectAsync(
            $"hub3-{Guid.NewGuid():N}@test.com", "HostPlayer3");

        try
        {
            var createResp = await restClient.PostAsync("/api/rooms", null);
            var room = await createResp.Content.ReadFromJsonAsync<CreateRoomResponse>();

            RoomStateDto? joinState = null;
            hub.On<RoomStateDto>("RoomJoined", s => joinState = s);
            await hub.SendAsync("JoinRoom", room!.RoomCode);
            await WaitUntil(() => joinState is not null);

            RoundStartedMessage? roundMsg = null;
            hub.On<RoundStartedMessage>("RoundStarted", msg => roundMsg = msg);

            await hub.SendAsync("StartRound", room.RoomCode);
            await WaitUntil(() => roundMsg is not null, timeoutMs: 5000);

            Assert.NotNull(roundMsg);
            Assert.InRange(roundMsg!.Letter, 'A', 'Z');
            Assert.True(roundMsg.EndsAt > DateTime.UtcNow);
        }
        finally
        {
            await hub.DisposeAsync();
            restClient.Dispose();
        }
    }

    [Fact]
    public async Task SubmitAnswers_AfterRoundStarts_ReceivesRoundResults()
    {
        var (restClient, hub) = await ConnectAsync(
            $"hub4-{Guid.NewGuid():N}@test.com", "SubmitPlayer4");

        try
        {
            var createResp = await restClient.PostAsync("/api/rooms", null);
            var room = await createResp.Content.ReadFromJsonAsync<CreateRoomResponse>();

            RoomStateDto? joinState = null;
            hub.On<RoomStateDto>("RoomJoined", s => joinState = s);
            await hub.SendAsync("JoinRoom", room!.RoomCode);
            await WaitUntil(() => joinState is not null);

            hub.On<RoundStartedMessage>("RoundStarted", _ => { });
            await hub.SendAsync("StartRound", room.RoomCode);
            await WaitUntil(() => false, 200); // short delay for round to propagate

            RoundResultsMessage? results = null;
            hub.On<RoundResultsMessage>("RoundResults", msg => results = msg);

            await hub.SendAsync("SubmitAnswers", room.RoomCode,
                new SubmitAnswersRequest("London", "London", "London", "London", "London"));

            await WaitUntil(() => results is not null, timeoutMs: 10000);

            Assert.NotNull(results);
            Assert.Single(results!.Results);
            Assert.Equal(1, results.Results[0].Rank);
        }
        finally
        {
            await hub.DisposeAsync();
            restClient.Dispose();
        }
    }

    private static async Task WaitUntil(Func<bool> condition, int timeoutMs = 3000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(50);
    }

    /// <summary>
    /// Wraps the TestServer's inner handler and maintains a simple cookie store
    /// so that Set-Cookie headers from REST calls are automatically included in
    /// subsequent SignalR negotiate/poll requests.
    /// Avoids <see cref="CookieContainer"/> which can silently drop cookies with
    /// non-standard attributes such as <c>SameSite=Strict</c>.
    /// </summary>
    private sealed class SharedCookieHandler : DelegatingHandler
    {
        private readonly Dictionary<string, string> _cookies = new();
        private readonly object _lock = new();

        public SharedCookieHandler(HttpMessageHandler inner) : base(inner) { }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string cookieHeader;
            lock (_lock)
            {
                cookieHeader = string.Join("; ",
                    _cookies.Select(kv => $"{kv.Key}={kv.Value}"));
            }

            if (!string.IsNullOrEmpty(cookieHeader))
                request.Headers.Add("Cookie", cookieHeader);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                lock (_lock)
                {
                    foreach (var raw in setCookies)
                    {
                        // "name=value; Path=/; SameSite=Strict; HttpOnly"
                        var nameValue = raw.Split(';')[0].Trim();
                        var eq = nameValue.IndexOf('=');
                        if (eq > 0)
                        {
                            var name = nameValue[..eq].Trim();
                            var value = nameValue[(eq + 1)..].Trim();
                            if (!string.IsNullOrEmpty(name))
                                _cookies[name] = value;
                        }
                    }
                }
            }

            return response;
        }
    }
}
