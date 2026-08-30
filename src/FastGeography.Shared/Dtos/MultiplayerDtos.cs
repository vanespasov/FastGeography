namespace FastGeography.Shared.Dtos;

using FastGeography.Shared;

public record CreateRoomResponse(string RoomCode, string LanguageCode);

/// <summary>One completed round row belonging to a single player (letter + their scored answers).</summary>
public record CompletedRoundRow(char Letter, List<LocationResult> Details);

public record RoomStateDto(
    string RoomCode,
    List<string> Players,
    string HostName,
    bool RoundActive,
    int RoundsCompletedInSet,
    bool SetComplete,
    /// <summary>Caller-only: their own completed rows for the current set.</summary>
    List<CompletedRoundRow> MyCompletedRounds,
    /// <summary>ISO game language code for this room ("en" or "mk").</summary>
    string LanguageCode);

public record RoundStartedMessage(char Letter, DateTime EndsAt, int RoundNumber);

public record PlayerRoundResult(
    string PlayerName,
    int TotalPoints,
    int Rank,
    List<LocationResult> Details);

public record RoundResultsMessage(
    List<PlayerRoundResult> Results,
    int RoundsCompletedInSet,
    bool SetComplete);
