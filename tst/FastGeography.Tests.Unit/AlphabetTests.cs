namespace FastGeography.Tests.Unit;

using FastGeography.Shared;

/// <summary>
/// Verifies <see cref="Alphabet"/> and <see cref="GameLanguage"/> helpers.
/// </summary>
public sealed class AlphabetTests
{
    // ── Letter sets ────────────────────────────────────────────────────────

    [Fact]
    public void EnglishLetters_Contains26LatinUpperCaseLetters()
    {
        var letters = Alphabet.EnglishLetters;
        Assert.Equal(26, letters.Count);
        Assert.All(letters, c => Assert.InRange(c, 'A', 'Z'));
    }

    [Fact]
    public void MacedonianLetters_Contains31Letters()
    {
        var letters = Alphabet.MacedonianLetters;
        Assert.Equal(31, letters.Count);
    }

    [Fact]
    public void MacedonianLetters_AreAllDistinct()
    {
        var letters = Alphabet.MacedonianLetters;
        Assert.Equal(letters.Count, letters.Distinct().Count());
    }

    [Theory]
    [InlineData('А')]
    [InlineData('Б')]
    [InlineData('Ш')]
    [InlineData('Ѓ')]
    [InlineData('Ѕ')]
    [InlineData('Ј')]
    [InlineData('Љ')]
    [InlineData('Њ')]
    [InlineData('Ќ')]
    [InlineData('Џ')]
    public void MacedonianLetters_ContainsExpectedCyrillicCharacters(char letter)
    {
        Assert.Contains(letter, Alphabet.MacedonianLetters);
    }

    // ── RandomLetter ───────────────────────────────────────────────────────

    [Fact]
    public void RandomLetter_English_IsInLatinAlphabet()
    {
        for (int i = 0; i < 100; i++)
        {
            var c = Alphabet.RandomLetter(GameLanguage.En);
            Assert.Contains(c, Alphabet.EnglishLetters);
        }
    }

    [Fact]
    public void RandomLetter_Macedonian_IsInMacedonianAlphabet()
    {
        for (int i = 0; i < 100; i++)
        {
            var c = Alphabet.RandomLetter(GameLanguage.Mk);
            Assert.Contains(c, Alphabet.MacedonianLetters);
        }
    }

    [Fact]
    public void RandomLetter_Macedonian_NeverReturnsLatinLetter()
    {
        for (int i = 0; i < 200; i++)
        {
            var c = Alphabet.RandomLetter(GameLanguage.Mk);
            Assert.DoesNotContain(c, Alphabet.EnglishLetters);
        }
    }

    // ── StartsWithLetter ───────────────────────────────────────────────────

    [Theory]
    [InlineData("London", 'L')]
    [InlineData("LONDON", 'L')]
    [InlineData("london", 'L')]
    public void StartsWithLetter_LatinCaseInsensitive_ReturnsTrue(string answer, char letter)
    {
        Assert.True(Alphabet.StartsWithLetter(answer, letter));
    }

    [Theory]
    [InlineData("Скопје", 'С')]
    [InlineData("скопје", 'С')]
    [InlineData("СКОПЈЕ", 'С')]
    public void StartsWithLetter_CyrillicCaseInsensitive_ReturnsTrue(string answer, char letter)
    {
        Assert.True(Alphabet.StartsWithLetter(answer, letter));
    }

    [Theory]
    [InlineData("London", 'B')]
    [InlineData("Скопје", 'А')]
    public void StartsWithLetter_WrongLetter_ReturnsFalse(string answer, char letter)
    {
        Assert.False(Alphabet.StartsWithLetter(answer, letter));
    }

    // ── GameLanguage helpers ───────────────────────────────────────────────

    [Theory]
    [InlineData("en", GameLanguage.En)]
    [InlineData("EN", GameLanguage.En)]
    [InlineData("mk", GameLanguage.Mk)]
    [InlineData("MK", GameLanguage.Mk)]
    [InlineData("fr", GameLanguage.En)]
    [InlineData(null, GameLanguage.En)]
    [InlineData("", GameLanguage.En)]
    public void Parse_ReturnsExpected(string? input, GameLanguage expected)
    {
        Assert.Equal(expected, GameLanguageExtensions.Parse(input));
    }

    [Fact]
    public void ToCode_English_ReturnsEn()
    {
        Assert.Equal("en", GameLanguage.En.ToCode());
    }

    [Fact]
    public void ToCode_Macedonian_ReturnsMk()
    {
        Assert.Equal("mk", GameLanguage.Mk.ToCode());
    }
}
