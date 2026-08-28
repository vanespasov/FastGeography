namespace FastGeography.Shared.Dtos;

public record LeaderboardEntry(
    int Rank,
    string DisplayName,
    int CareerPoints,
    string Badge,
    int GamesPlayed);

public record RecentRound(
    Guid RoundId,
    string Mode,
    char Letter,
    int Points,
    DateTime PlayedAt);

public record PlayerStats(
    int Rank,
    string DisplayName,
    int CareerPoints,
    string Badge,
    int GamesPlayed,
    List<RecentRound> RecentRounds);
