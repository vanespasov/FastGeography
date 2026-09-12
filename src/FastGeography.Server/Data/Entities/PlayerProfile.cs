namespace FastGeography.Server.Data.Entities;

using FastGeography.Server.Data;

public sealed class PlayerProfile
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int CareerPoints { get; set; }
    public int GamesPlayed { get; set; }
}
