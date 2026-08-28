namespace FastGeography.Server.Data.Entities;

using FastGeography.Shared;

/// <summary>
/// A geography toponym that has been verified by a maps provider and acts as the
/// server-side cache of correct answers.  Populated on first confirmed hit from
/// Bing Maps so subsequent validations skip the external API entirely.
/// </summary>
public sealed class Toponym
{
    public Guid Id { get; set; }

    /// <summary>
    /// Lower-invariant, trimmed form of the player's answer used as the lookup key.
    /// </summary>
    public string NormalizedName { get; set; } = string.Empty;

    /// <summary>Display form as submitted by the player (or formatted by the provider).</summary>
    public string DisplayName { get; set; } = string.Empty;

    public LocationType Category { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>Name of the maps provider that verified this entry (e.g. "Bing").</summary>
    public string Provider { get; set; } = "Bing";

    /// <summary>UTC timestamp when the provider confirmed the toponym.</summary>
    public DateTime VerifiedAtUtc { get; set; }
}
