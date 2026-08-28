namespace FastGeography.Server.Services;

using System.Collections.Concurrent;

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

    /// <summary>Map: userId → submitted answers once submitted this round.</summary>
    public ConcurrentDictionary<string, object> Submissions { get; } = new();

    public CancellationTokenSource? RoundTimerCts { get; set; }
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
}

public interface IRoomService
{
    GameRoom CreateRoom(string hostUserId, string hostName);
    GameRoom? GetRoom(string code);
    bool TryJoin(string code, string userId, string displayName, string connectionId);
    void Leave(string userId, string connectionId);
    void Cleanup();
}

public sealed class RoomService : IRoomService
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();

    private static readonly TimeSpan RoomTtl = TimeSpan.FromHours(2);
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public GameRoom CreateRoom(string hostUserId, string hostName)
    {
        Cleanup();
        var code = GenerateCode();
        var room = new GameRoom
        {
            Code = code,
            HostUserId = hostUserId,
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
