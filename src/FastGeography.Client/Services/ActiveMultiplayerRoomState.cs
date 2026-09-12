namespace FastGeography.Client.Services;

using System.Text.Json;

using Microsoft.JSInterop;

/// <summary>
/// Remembers the last multiplayer room the player joined so they can return
/// after accidental navigation away. Cleared on explicit leave or expiry.
/// </summary>
public sealed class ActiveMultiplayerRoomState
{
    private const string StorageKey = "fg_mp_room";
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(2);

    private readonly IJSRuntime _js;
    private bool _loaded;

    public ActiveMultiplayerRoomState(IJSRuntime js) => _js = js;

    public string? RoomCode { get; private set; }

    public event Action? Changed;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;

        var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (!TryParse(json, out var code))
        {
            RoomCode = null;
            _loaded = true;
            return;
        }

        RoomCode = code;
        _loaded = true;
    }

    public async Task SetAsync(string roomCode)
    {
        var normalized = roomCode.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalized)) return;

        var payload = JsonSerializer.Serialize(new StoredRoom(normalized, DateTime.UtcNow));
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, payload);
        RoomCode = normalized;
        _loaded = true;
        Changed?.Invoke();
    }

    public async Task ClearAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        RoomCode = null;
        _loaded = true;
        Changed?.Invoke();
    }

    private static bool TryParse(string? json, out string code)
    {
        code = string.Empty;
        if (string.IsNullOrWhiteSpace(json)) return false;

        try
        {
            var stored = JsonSerializer.Deserialize<StoredRoom>(json);
            if (stored is null || string.IsNullOrWhiteSpace(stored.Code)) return false;
            if (DateTime.UtcNow - stored.JoinedAtUtc > MaxAge) return false;

            code = stored.Code.Trim().ToUpperInvariant();
            return code.Length > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record StoredRoom(string Code, DateTime JoinedAtUtc);
}
