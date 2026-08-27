namespace FastGeography.Shared;

/// <summary>
/// Single source of truth for all point values and game configuration constants.
/// Both client-side pre-validation and server-side geocode results use these values.
/// </summary>
public static class ScoringRules
{
    /// <summary>Points awarded when an answer matches the expected location type via Bing Maps.</summary>
    public const int ValidPoints = 20;

    /// <summary>Points deducted when Bing Maps cannot find a matching location of the requested type.</summary>
    public const int InvalidPoints = -5;

    /// <summary>Points deducted when the answer does not start with the required letter.</summary>
    public const int WrongLetterPoints = -10;

    /// <summary>Points for a blank answer (no penalty, no reward).</summary>
    public const int EmptyPoints = 0;

    /// <summary>Default countdown duration in seconds.</summary>
    public const int DefaultTimerSeconds = 60;

    /// <summary>Maximum allowed length for a location answer to guard against abuse.</summary>
    public const int MaxAnswerLength = 100;

    /// <summary>Score threshold above which the achievement banner is shown.</summary>
    public const int AchievementThreshold = 0;

    /// <summary>Returns the CSS row-colour class for a given point value.</summary>
    public static string CssRowClass(int points) =>
        points == 0 ? "table-light" : points > 0 ? "table-success" : "table-danger";
}
