namespace FastGeography.Shared;

/// <summary>
/// Supported game languages. Controls the letter alphabet drawn for rounds and
/// the locale passed to geocoding providers when validating player answers.
/// </summary>
public enum GameLanguage
{
    /// <summary>English — Latin alphabet (A–Z).</summary>
    En = 0,

    /// <summary>Macedonian — Cyrillic alphabet (31 letters).</summary>
    Mk = 1
}

public static class GameLanguageExtensions
{
    /// <summary>Returns the ISO 639-1 code ("en" or "mk").</summary>
    public static string ToCode(this GameLanguage language) => language switch
    {
        GameLanguage.Mk => "mk",
        _ => "en"
    };

    /// <summary>Parses an ISO code; unknown values fall back to English.</summary>
    public static GameLanguage Parse(string? code) =>
        string.Equals(code, "mk", StringComparison.OrdinalIgnoreCase) ? GameLanguage.Mk : GameLanguage.En;
}
