namespace FastGeography.Server.Data.Seed;

using System.Security.Cryptography;
using System.Text;

using FastGeography.Shared;

/// <summary>
/// A single entry in the well-known toponym catalog.  Ids are deterministic
/// (MD5 of the lookup key) so re-running the seeder never creates duplicate PKs.
/// </summary>
public sealed record ToponymSeedRecord
{
    private static readonly DateTime SeedVerifiedAt =
        new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public ToponymSeedRecord(
        string displayName,
        LocationType category,
        string languageCode,
        double latitude,
        double longitude)
    {
        DisplayName  = displayName;
        NormalizedName = displayName.Trim().ToLowerInvariant();
        Category     = category;
        LanguageCode = languageCode;
        Latitude     = latitude;
        Longitude    = longitude;
        Id           = ComputeId(NormalizedName, category, languageCode);
        VerifiedAtUtc = SeedVerifiedAt;
    }

    public Guid         Id            { get; }
    public string       DisplayName   { get; }
    public string       NormalizedName{ get; }
    public LocationType Category      { get; }
    public string       LanguageCode  { get; }
    public double       Latitude      { get; }
    public double       Longitude     { get; }
    public DateTime     VerifiedAtUtc { get; }
    public string       Provider      => "Seed";

    private static Guid ComputeId(string normalized, LocationType category, string lang)
    {
        var input = $"{normalized}|{(int)category}|{lang}";
        var hash  = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}
