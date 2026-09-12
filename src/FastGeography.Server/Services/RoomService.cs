namespace FastGeography.Server.Services;

using System.Collections.Concurrent;

using FastGeography.Shared;
using FastGeography.Shared.Dtos;

public sealed class GameRoom
{
    public string Code { get; set; } = string.Empty;

    /// <summary>UserId of the host.</summary>
    public string HostUserId { get; set; } = string.Empty;

    /// <summary>Map: userId → displayName for all players currently in the room.</summary>
    public ConcurrentDictionary<string, string> Players { get; } = new();

    /// <summary>Map: userId → connectionId (updated when player reconnects).</summary>
    public ConcurrentDictionary<string, string> Connections { get; } = new();

    public char? CurrentLetter { get; set; }
    public DateTime? RoundEndsAt { get; set; }
    public bool RoundActive { get; set; }

    /// <summary>ISO 639-1 game language code ("en" or "mk") set by the host when the room is created.</summary>
    public string LanguageCode { get; set; } = "en";

    /// <summary>Map: userId → submitted answers once submitted this round.</summary>
    public ConcurrentDictionary<string, object> Submissions { get; } = new();

    public CancellationTokenSource? RoundTimerCts { get; set; }
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    // ── 5-round set tracking ───────────────────────────────────────────────

    /// <summary>How many rounds have been finalised in the current set (0–SetSize).</summary>
    public int RoundsCompletedInSet { get; set; }

    /// <summary>True once all rounds in the set have been played.</summary>
    public bool SetComplete => RoundsCompletedInSet >= ScoringRules.SetSize;

    /// <summary>Map: userId → ordered list of that player's completed round rows for this set.</summary>
    public ConcurrentDictionary<string, List<CompletedRoundRow>> PlayerSetHistory { get; } = new();
}

public interface IRoomService
{
    GameRoom CreateRoom(string hostUserId, string hostName, GameLanguage language = GameLanguage.En);
    GameRoom? GetRoom(string code);
    bool TryJoin(string code, string userId, string displayName, string connectionId);
    void Leave(string userId, string connectionId);
    GameRoom? FindRoomByConnection(string connectionId);
    void Cleanup();
}

public sealed class RoomService : IRoomService
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();

    private static readonly TimeSpan RoomTtl = TimeSpan.FromHours(2);
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public GameRoom CreateRoom(string hostUserId, string hostName, GameLanguage language = GameLanguage.En)
    {
        Cleanup();
        var code = GenerateCode();
        var room = new GameRoom
        {
            Code = code,
            HostUserId = hostUserId,
            LanguageCode = language.ToCode(),
            LastActivity = DateTime.UtcNow
        };
        room.Players[hostUserId] = hostName;
        _rooms[code] = room;
        return room;
    }

    public GameRoom? GetRoom(string code) =>
        _rooms.TryGetValue(code.ToUpperInvariant(), out var room) ? room : null;

    public bool TryJoin(string code, string userId, string displayName, string connectionId)
    {
        var room = GetRoom(code);
        if (room is null) return false;

        room.Players[userId] = displayName;
        room.Connections[userId] = connectionId;
        room.LastActivity = DateTime.UtcNow;
        return true;
    }

    public void Leave(string userId, string connectionId)
    {
        foreach (var room in _rooms.Values)
        {
            if (room.Connections.TryGetValue(userId, out var cid) && cid == connectionId)
            {
                room.Players.TryRemove(userId, out _);
                room.Connections.TryRemove(userId, out _);
                room.LastActivity = DateTime.UtcNow;

                if (room.Players.IsEmpty)
                {
                    _rooms.TryRemove(room.Code, out _);
                    room.RoundTimerCts?.Cancel();
                }
                break;
            }
        }
    }

    public GameRoom? FindRoomByConnection(string connectionId) =>
        _rooms.Values.FirstOrDefault(r => r.Connections.Values.Contains(connectionId));

    public void Cleanup()
    {
        var cutoff = DateTime.UtcNow - RoomTtl;
        foreach (var (key, room) in _rooms)
        {
            if (room.LastActivity < cutoff)
            {
                _rooms.TryRemove(key, out _);
                room.RoundTimerCts?.Cancel();
            }
        }
    }

    private string GenerateCode()
    {
        string code;
        do
        {
            code = new string(Enumerable.Range(0, 6).Select(_ => Chars[Random.Shared.Next(Chars.Length)]).ToArray());
        } while (_rooms.ContainsKey(code));
        return code;
    }
}
