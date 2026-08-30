namespace FastGeography.Server.Data.Entities;

using FastGeography.Shared;

public sealed class GameRound
{
    public Guid Id { get; set; }
    public GameMode Mode { get; set; }
    public char Letter { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime EndsAt { get; set; }

    /// <summary>Short alphanumeric code for multiplayer rooms; null for solo rounds.</summary>
    public string? RoomCode { get; set; }

    /// <summary>ISO 639-1 game language code ("en" or "mk") for the alphabet used in this round.</summary>
    public string LanguageCode { get; set; } = "en";

    public List<RoundSubmission> Submissions { get; set; } = [];
}
