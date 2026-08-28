namespace FastGeography.Shared.Dtos;

using FastGeography.Shared;

public record CreateRoomResponse(string RoomCode);

public record RoomStateDto(
    string RoomCode,
    List<string> Players,
    string HostName,
    bool RoundActive);

public record RoundStartedMessage(char Letter, DateTime EndsAt);

public record PlayerRoundResult(
    string PlayerName,
    int TotalPoints,
    int Rank,
    List<LocationResult> Details);

public record RoundResultsMessage(List<PlayerRoundResult> Results);
