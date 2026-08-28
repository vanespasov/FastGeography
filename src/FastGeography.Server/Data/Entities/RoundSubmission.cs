namespace FastGeography.Server.Data.Entities;

using FastGeography.Server.Data;

public sealed class RoundSubmission
{
    public Guid Id { get; set; }
    public Guid RoundId { get; set; }
    public GameRound Round { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public string? CityAnswer { get; set; }
    public string? VillageAnswer { get; set; }
    public string? CountryAnswer { get; set; }
    public string? RiverAnswer { get; set; }
    public string? MountainAnswer { get; set; }

    public int CityPoints { get; set; }
    public int VillagePoints { get; set; }
    public int CountryPoints { get; set; }
    public int RiverPoints { get; set; }
    public int MountainPoints { get; set; }

    public int TotalPoints =>
        CityPoints + VillagePoints + CountryPoints + RiverPoints + MountainPoints;

    public int SecondsPlayed { get; set; }

    /// <summary>Rank within this round (1 = highest score).</summary>
    public int RankInRound { get; set; }
}
