namespace FastGeography.Server.Data.Entities;

using FastGeography.Shared;

/// <summary>
/// One AI-generated travel micro-story for a verified place.
/// Several rows may exist per (NormalizedName, Category, LanguageCode) to form a pool.
/// Stories are stored independently of a language-specific <see cref="Toponym"/> row
/// so a Macedonian story can persist when only an English toponym exists.
/// </summary>
public sealed class ToponymStory
{
    public Guid Id { get; set; }

    public string NormalizedName { get; set; } = string.Empty;

    public LocationType Category { get; set; }

    /// <summary>ISO 639-1 game language of the story body ("en" or "mk").</summary>
    public string LanguageCode { get; set; } = "en";

    /// <summary>The 40–70 word story paragraph.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Prompt angle used when generating (e.g. "history"). Null for migrated stories.</summary>
    public string? Angle { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
