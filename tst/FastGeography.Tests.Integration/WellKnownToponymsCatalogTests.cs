namespace FastGeography.IntegrationTests;

using FastGeography.Server.Data.Seed;
using FastGeography.Shared;

/// <summary>
/// Unit-style tests for <see cref="WellKnownToponyms"/> — no database required.
/// Verifies uniqueness, validity, bilingual presence, and coverage against the
/// documented gap allow-list.
/// </summary>
public sealed class WellKnownToponymsCatalogTests
{
    private static readonly IReadOnlyList<ToponymSeedRecord> All = WellKnownToponyms.All;

    // ── Basic integrity ─────────────────────────────────────────────────

    [Fact]
    public void All_HasEntries()
    {
        Assert.NotEmpty(All);
    }

    [Fact]
    public void All_NoEmptyDisplayNames()
    {
        Assert.All(All, r => Assert.False(string.IsNullOrWhiteSpace(r.DisplayName)));
    }

    [Fact]
    public void All_NormalizedNameIsLowerInvariantTrimmedDisplayName()
    {
        Assert.All(All, r =>
            Assert.Equal(r.DisplayName.Trim().ToLowerInvariant(), r.NormalizedName));
    }

    [Fact]
    public void All_LatLonInValidRange()
    {
        Assert.All(All, r =>
        {
            Assert.InRange(r.Latitude,  -90.0,  90.0);
            Assert.InRange(r.Longitude, -180.0, 180.0);
        });
    }

    [Fact]
    public void All_ProviderIsSeed()
    {
        Assert.All(All, r => Assert.Equal("Seed", r.Provider));
    }

    [Fact]
    public void All_VerifiedAtUtcIsFixedDeterministicDate()
    {
        var expected = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.All(All, r => Assert.Equal(expected, r.VerifiedAtUtc));
    }

    // ── Uniqueness ──────────────────────────────────────────────────────

    [Fact]
    public void All_LookupKeyIsUnique()
    {
        var seen = new HashSet<(string, int, string)>();
        var duplicates = new List<string>();

        foreach (var r in All)
        {
            var key = (r.NormalizedName, (int)r.Category, r.LanguageCode);
            if (!seen.Add(key))
                duplicates.Add($"({r.NormalizedName},{r.Category},{r.LanguageCode})");
        }

        Assert.Empty(duplicates);
    }

    [Fact]
    public void All_IdIsDeterministicAndUnique()
    {
        var ids = All.Select(r => r.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());

        // Recreating a record gives the same Id.
        var first = All[0];
        var recreated = new ToponymSeedRecord(
            first.DisplayName, first.Category, first.LanguageCode,
            first.Latitude, first.Longitude);
        Assert.Equal(first.Id, recreated.Id);
    }

    // ── Bilingual presence ──────────────────────────────────────────────

    [Fact]
    public void All_ContainsEntriesForBothLanguages()
    {
        var langs = All.Select(r => r.LanguageCode).ToHashSet();
        Assert.Contains("en", langs);
        Assert.Contains("mk", langs);
    }

    [Theory]
    [InlineData(LocationType.City)]
    [InlineData(LocationType.Village)]
    [InlineData(LocationType.Country)]
    [InlineData(LocationType.River)]
    [InlineData(LocationType.Mountain)]
    public void AllCategories_HaveEntriesInBothLanguages(LocationType cat)
    {
        var langs = All.Where(r => r.Category == cat).Select(r => r.LanguageCode).ToHashSet();
        Assert.Contains("en", langs);
        Assert.Contains("mk", langs);
    }

    // ── Coverage — English (A–Z) ────────────────────────────────────────

    /// <summary>
    /// Allowed gaps: EN Country has no universally-recognised sovereign state
    /// starting with W or X.
    /// </summary>
    private static readonly HashSet<(char, LocationType)> EnAllowedGaps = new()
    {
        ('W', LocationType.Country),
        ('X', LocationType.Country),
    };

    [Theory]
    [InlineData(LocationType.City)]
    [InlineData(LocationType.Village)]
    [InlineData(LocationType.Country)]
    [InlineData(LocationType.River)]
    [InlineData(LocationType.Mountain)]
    public void EnglishCoverage_EveryLetterHasAtLeastOneEntry_ExceptAllowedGaps(LocationType cat)
    {
        var covered = All
            .Where(r => r.LanguageCode == "en" && r.Category == cat)
            .Select(r => char.ToUpperInvariant(r.NormalizedName[0]))
            .ToHashSet();

        var missing = Alphabet.EnglishLetters
            .Where(letter => !covered.Contains(letter) && !EnAllowedGaps.Contains((letter, cat)))
            .ToList();

        Assert.Empty(missing);
    }

    // ── Coverage — Macedonian (А–Ш) ─────────────────────────────────────

    /// <summary>
    /// Allow-list of (letter, category) pairs that have no real entry due to
    /// the scarcity of place names starting with rare Macedonian Cyrillic letters.
    /// </summary>
    private static readonly HashSet<(char, LocationType)> MkAllowedGaps = new()
    {
        // Cities
        ('Ѕ', LocationType.City),
        ('Ќ', LocationType.City),
        // Villages
        ('Ѕ', LocationType.Village),
        ('Ќ', LocationType.Village),
        ('Њ', LocationType.Village),
        ('Џ', LocationType.Village),
        // Countries
        ('Ж', LocationType.Country),
        ('Ѓ', LocationType.Country),
        ('Ѕ', LocationType.Country),
        ('Љ', LocationType.Country),
        ('Њ', LocationType.Country),
        ('Ќ', LocationType.Country),
        // Rivers
        ('Ѓ', LocationType.River),
        ('Ѕ', LocationType.River),
        ('Ќ', LocationType.River),
        ('Љ', LocationType.River),
        ('Њ', LocationType.River),
        ('Ш', LocationType.River),
        ('Џ', LocationType.River),
        // Mountains
        ('Ж', LocationType.Mountain),
        ('З', LocationType.Mountain),
        ('Ѓ', LocationType.Mountain),
        ('Ѕ', LocationType.Mountain),
        ('Ќ', LocationType.Mountain),
        ('Љ', LocationType.Mountain),
        ('Њ', LocationType.Mountain),
        ('Џ', LocationType.Mountain),
    };

    [Theory]
    [InlineData(LocationType.City)]
    [InlineData(LocationType.Village)]
    [InlineData(LocationType.Country)]
    [InlineData(LocationType.River)]
    [InlineData(LocationType.Mountain)]
    public void MacedonianCoverage_EveryLetterHasAtLeastOneEntry_ExceptAllowedGaps(LocationType cat)
    {
        var covered = All
            .Where(r => r.LanguageCode == "mk" && r.Category == cat)
            .Select(r => r.NormalizedName[0])
            .Select(char.ToUpperInvariant)
            .ToHashSet();

        // The Macedonian letters are uppercase; normalised names are lowercase —
        // compare against the uppercase form of the first char.
        var missing = Alphabet.MacedonianLetters
            .Where(letter => !covered.Contains(letter) && !MkAllowedGaps.Contains((letter, cat)))
            .ToList();

        Assert.Empty(missing);
    }

    // ── Gap allow-list is tight (no unnecessary exceptions) ────────────

    [Fact]
    public void EnAllowedGaps_AreActuallyMissing()
    {
        foreach (var (letter, cat) in EnAllowedGaps)
        {
            var hasCoverage = All.Any(r =>
                r.LanguageCode == "en" &&
                r.Category == cat &&
                char.ToUpperInvariant(r.NormalizedName[0]) == letter);

            Assert.False(hasCoverage,
                $"Gap ({letter}, {cat}, en) is in the allow-list but IS covered — remove it.");
        }
    }

    [Fact]
    public void MkAllowedGaps_AreActuallyMissing()
    {
        foreach (var (letter, cat) in MkAllowedGaps)
        {
            var hasCoverage = All.Any(r =>
                r.LanguageCode == "mk" &&
                r.Category == cat &&
                char.ToUpperInvariant(r.NormalizedName[0]) == letter);

            Assert.False(hasCoverage,
                $"Gap ({letter}, {cat}, mk) is in the allow-list but IS covered — remove it.");
        }
    }
}
