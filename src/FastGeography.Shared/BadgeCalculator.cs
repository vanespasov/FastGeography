namespace FastGeography.Shared;

/// <summary>
/// Determines the explorer badge for a cumulative point total.
/// Extracted from GameTable so the logic can be unit-tested and reused.
/// </summary>
public static class BadgeCalculator
{
    private static readonly (int MaxInclusive, Badge Badge)[] Tiers =
    [
        (100,  Badge.Junior),
        (200,  Badge.Cadet),
        (300,  Badge.Explorer),
        (400,  Badge.Traveller),
        (500,  Badge.Jumper),
        (600,  Badge.EarthSurfer),
        (700,  Badge.EarthConqueror),
        (800,  Badge.SolarSpectre),
        (900,  Badge.GalacticSurfer),
        (1000, Badge.GalacticConqueror),
    ];

    /// <summary>
    /// Returns the badge for <paramref name="totalPoints"/>.
    /// Any score above 1000 stays at GalacticConqueror; negative scores show Junior.
    /// </summary>
    public static Badge Calculate(int totalPoints)
    {
        if (totalPoints <= 0) return Badge.Junior;

        foreach (var (max, badge) in Tiers)
        {
            if (totalPoints <= max)
                return badge;
        }

        return Badge.GalacticConqueror;
    }
}
