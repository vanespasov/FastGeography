namespace FastGeography.Server.Options;

public sealed class BingMapsOptions
{
    public const string Section = "BingMaps";

    /// <summary>
    /// Bing Maps API key. Set via User Secrets or the BINGMAPS__APIKEY environment variable.
    /// Never commit a real key to source control.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
