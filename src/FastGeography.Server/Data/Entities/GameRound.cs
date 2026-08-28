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

    public List<RoundSubmission> Submissions { get; set; } = [];
}
