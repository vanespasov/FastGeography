namespace FastGeography.Shared;

/// <summary>
/// Letter sets and random-letter selection for each supported game language.
/// All server-side and client-side letter draws go through this class so the
/// alphabet is defined in one place.
/// </summary>
public static class Alphabet
{
    /// <summary>Latin A–Z letters used in English mode.</summary>
    public static readonly IReadOnlyList<char> EnglishLetters =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

    /// <summary>
    /// The standard 31-letter Macedonian Cyrillic alphabet in upper case.
    /// Ѓ, Ѕ, Ј, Љ, Њ, Ќ, Џ are included — all are single Unicode code points.
    /// </summary>
    public static readonly IReadOnlyList<char> MacedonianLetters =
        "АБВГДЃЕЖЗЅИЈКЛЉМНЊОПРСТЌУФХЦЧЏШ".ToCharArray();

    /// <summary>Returns the letter set for the given language.</summary>
    public static IReadOnlyList<char> Letters(GameLanguage language) => language switch
    {
        GameLanguage.Mk => MacedonianLetters,
        _ => EnglishLetters
    };

    /// <summary>Draws a uniformly random letter from the alphabet of <paramref name="language"/>.</summary>
    public static char RandomLetter(GameLanguage language)
    {
        var letters = Letters(language);
        return letters[Random.Shared.Next(letters.Count)];
    }

    /// <summary>
    /// Returns true when <paramref name="answer"/> starts with the round
    /// <paramref name="letter"/> (case-insensitive, culture-invariant).
    /// Works for both Latin and Cyrillic because OrdinalIgnoreCase covers the
    /// case pairs defined in the Macedonian Cyrillic block.
    /// </summary>
    public static bool StartsWithLetter(string answer, char letter) =>
        answer.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase);
}
