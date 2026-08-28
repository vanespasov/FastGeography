namespace FastGeography.Server.Hubs;

using System.Security.Claims;

using FastGeography.Server.Data;
using FastGeography.Server.Data.Entities;
using FastGeography.Server.Services;
using FastGeography.Shared;
using FastGeography.Shared.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

[Authorize]
public sealed class GameHub : Hub
{
    private readonly IRoomService _roomService;
    private readonly IGeocodingService _geocodingService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GameHub> _logger;

    public GameHub(
        IRoomService roomService,
        IGeocodingService geocodingService,
        ApplicationDbContext db,
        ILogger<GameHub> logger)
    {
        _roomService = roomService;
        _geocodingService = geocodingService;
        _db = db;
        _logger = logger;
    }

    // ── Connection lifecycle ────────────────────────────────────────────────

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = UserId();
        if (userId is not null)
        {
            // Find which room this connection was in and notify others.
            // RoomService.Leave will remove the player from the room.
            _roomService.Leave(userId, Context.ConnectionId);
        }
        return base.OnDisconnectedAsync(exception);
    }

    // ── Hub methods (client → server) ──────────────────────────────────────

    public async Task JoinRoom(string roomCode)
    {
        var userId = UserId();
        if (userId is null) { await SendError("Not authenticated."); return; }

        var displayName = DisplayName();
        var joined = _roomService.TryJoin(roomCode, userId, displayName, Context.ConnectionId);

        if (!joined) { await SendError($"Room '{roomCode}' not found."); return; }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupKey(roomCode));

        var room = _roomService.GetRoom(roomCode)!;
        var players = room.Players.Values.ToList();
        var hostName = room.Players.TryGetValue(room.HostUserId, out var h) ? h : "Host";

        await Clients.Caller.SendAsync("RoomJoined", new RoomStateDto(
            roomCode, players, hostName, room.RoundActive));

        await Clients.OthersInGroup(GroupKey(roomCode))
            .SendAsync("PlayerJoined", displayName);
    }

    public async Task StartRound(string roomCode)
    {
        var userId = UserId();
        var room = _roomService.GetRoom(roomCode);

        if (room is null) { await SendError("Room not found."); return; }
        if (room.HostUserId != userId) { await SendError("Only the host can start a round."); return; }
        if (room.RoundActive) { await SendError("A round is already active."); return; }
        if (room.Players.Count < 1) { await SendError("Need at least one player."); return; }

        var letter = (char)('A' + Random.Shared.Next(0, 26));
        var endsAt = DateTime.UtcNow.AddSeconds(ScoringRules.DefaultTimerSeconds);

        room.CurrentLetter = letter;
        room.RoundEndsAt = endsAt;
        room.RoundActive = true;
        room.Submissions.Clear();
        room.RoundTimerCts?.Cancel();
        room.RoundTimerCts = new CancellationTokenSource();

        await Clients.Group(GroupKey(roomCode))
            .SendAsync("RoundStarted", new RoundStartedMessage(letter, endsAt));

        _ = AutoScoreAfterDeadlineAsync(roomCode, room.RoundTimerCts.Token);
    }

    public async Task SubmitAnswers(string roomCode, SubmitAnswersRequest answers)
    {
        var userId = UserId();
        var room = _roomService.GetRoom(roomCode);

        if (room is null || !room.RoundActive) { await SendError("No active round."); return; }
        if (userId is null) { await SendError("Not authenticated."); return; }

        if (room.Submissions.ContainsKey(userId))
        {
            await SendError("Already submitted.");
            return;
        }

        room.Submissions[userId] = answers;
        await Clients.Caller.SendAsync("AnswersAccepted");

        if (room.Submissions.Count >= room.Players.Count)
        {
            await FinalizeRoundAsync(roomCode);
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task AutoScoreAfterDeadlineAsync(string roomCode, CancellationToken ct)
    {
        try
        {
            await Task.Delay(
                TimeSpan.FromSeconds(ScoringRules.DefaultTimerSeconds + 5), ct);

            if (!ct.IsCancellationRequested)
                await FinalizeRoundAsync(roomCode);
        }
        catch (TaskCanceledException) { }
    }

    private async Task FinalizeRoundAsync(string roomCode)
    {
        var room = _roomService.GetRoom(roomCode);
        if (room is null || !room.RoundActive) return;

        room.RoundActive = false;
        room.RoundTimerCts?.Cancel();

        var letter = room.CurrentLetter!.Value;

        // Validate all submissions in parallel
        var scoringTasks = room.Submissions
            .ToDictionary(
                kv => kv.Key,
                kv => ScoreSubmissionAsync(letter, (SubmitAnswersRequest)kv.Value));

        await Task.WhenAll(scoringTasks.Values);

        var rawResults = scoringTasks.Select(kv => (
            UserId: kv.Key,
            Details: kv.Value.Result,
            Total: kv.Value.Result.Sum(d => d.Points)
        )).ToList();

        // Assign ranks
        var ranked = rawResults.OrderByDescending(r => r.Total).ToList();
        var playerResults = new List<PlayerRoundResult>();

        for (int i = 0; i < ranked.Count; i++)
        {
            var entry = ranked[i];
            var playerName = room.Players.TryGetValue(entry.UserId, out var n) ? n : "Player";
            playerResults.Add(new PlayerRoundResult(playerName, entry.Total, i + 1, entry.Details));
        }

        await Clients.Group(GroupKey(roomCode))
            .SendAsync("RoundResults", new RoundResultsMessage(playerResults));

        await PersistMultiplayerRoundAsync(roomCode, ranked, letter);
    }

    private async Task<List<LocationResult>> ScoreSubmissionAsync(
        char letter, SubmitAnswersRequest req)
    {
        var tasks = new[]
        {
            ValidateOneAsync(LocationType.City,     req.City,     letter),
            ValidateOneAsync(LocationType.Village,  req.Village,  letter),
            ValidateOneAsync(LocationType.Country,  req.Country,  letter),
            ValidateOneAsync(LocationType.River,    req.River,    letter),
            ValidateOneAsync(LocationType.Mountain, req.Mountain, letter),
        };

        await Task.WhenAll(tasks);
        return tasks.Select(t => t.Result).ToList();
    }

    private async Task<LocationResult> ValidateOneAsync(
        LocationType type, string? answer, char letter)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return new LocationResult(type, answer, ScoringRules.EmptyPoints, null);

        if (!answer.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase))
            return new LocationResult(type, answer, ScoringRules.WrongLetterPoints, null);

        var result = await _geocodingService.ValidateAsync(answer, type);
        return new LocationResult(type, answer, result.Points, result.Coordinates);
    }

    private async Task PersistMultiplayerRoundAsync(
        string roomCode,
        List<(string UserId, List<LocationResult> Details, int Total)> ranked,
        char letter)
    {
        try
        {
            var round = new GameRound
            {
                Id = Guid.NewGuid(),
                Mode = GameMode.Multiplayer,
                Letter = letter,
                StartedAt = DateTime.UtcNow - TimeSpan.FromSeconds(ScoringRules.DefaultTimerSeconds),
                EndsAt = DateTime.UtcNow,
                RoomCode = roomCode
            };
            _db.GameRounds.Add(round);

            for (int i = 0; i < ranked.Count; i++)
            {
                var entry = ranked[i];
                var details = entry.Details;
                int P(LocationType t) => details.FirstOrDefault(d => d.Type == t)?.Points ?? 0;

                _db.RoundSubmissions.Add(new RoundSubmission
                {
                    Id = Guid.NewGuid(),
                    RoundId = round.Id,
                    UserId = entry.UserId,
                    CityPoints = P(LocationType.City),
                    VillagePoints = P(LocationType.Village),
                    CountryPoints = P(LocationType.Country),
                    RiverPoints = P(LocationType.River),
                    MountainPoints = P(LocationType.Mountain),
                    RankInRound = i + 1
                });

                var profile = await _db.PlayerProfiles.FirstOrDefaultAsync(p => p.UserId == entry.UserId);
                if (profile is not null)
                {
                    profile.CareerPoints += entry.Total;
                    profile.GamesPlayed++;
                }
            }

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist multiplayer round for room {RoomCode}", roomCode);
        }
    }

    private string? UserId() => Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    private string DisplayName() =>
        Context.User?.FindFirstValue("display_name")
        ?? Context.User?.FindFirstValue(ClaimTypes.Name)
        ?? Context.User?.FindFirstValue(ClaimTypes.Email)
        ?? "Player";

    private static string GroupKey(string roomCode) => $"room:{roomCode.ToUpperInvariant()}";

    private async Task SendError(string message) =>
        await Clients.Caller.SendAsync("Error", message);
}
