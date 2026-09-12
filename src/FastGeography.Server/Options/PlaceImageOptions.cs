namespace FastGeography.Server.Options;

/// <summary>Configuration for Wikipedia / Wikimedia Commons image lookups.</summary>
public sealed class PlaceImageOptions
{
    public const string Section = "PlaceImage";

    /// <summary>
    /// Required by Wikimedia API usage guidelines.
    /// </summary>
    public string UserAgent { get; set; } = "FastGeography/1.0 (https://github.com/FastGeography)";

    /// <summary>Days to wait before retrying a failed image lookup.</summary>
    public int MissRetryDays { get; set; } = 30;
}
