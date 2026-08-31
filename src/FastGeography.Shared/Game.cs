namespace FastGeography.Shared;

using System.Collections.Generic;

public class Game
{
    public Guid Id { get; set; }
    public DateTime DatePlayed { get; set; }
    public bool IsFinished { get; set; }
    public char Letter { get; set; }
    public GameLocation? City { get; set; }
    public GameLocation? Village { get; set; }
    public GameLocation? Country { get; set; }
    public GameLocation? Mountain { get; set; }
    public GameLocation? River { get; set; }

    public int SecondsPlayed { get; set; }

    public Dictionary<LocationType, int> PointsPerTerm { get; set; } = [];

    /// <summary>
    /// Sum of points across all five location categories.
    /// Null-safe: a missing location contributes 0 points.
    /// </summary>
    public int TotalPoints =>
        (City?.Points ?? 0) +
        (Village?.Points ?? 0) +
        (Country?.Points ?? 0) +
        (Mountain?.Points ?? 0) +
        (River?.Points ?? 0);

    /// <summary>Returns the Bootstrap row-colour CSS class for a given point value.</summary>
    public string SetCssClass(int points) => ScoringRules.CssRowClass(points);
}
