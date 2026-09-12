namespace FastGeography.Server.Options;

/// <summary>
/// Top-level geocoding configuration. Bind from the "Geocoding" config section.
/// </summary>
public sealed class GeocodingOptions
{
    public const string Section = "Geocoding";

    /// <summary>
    /// Active geocoding back-end. Recognised values (case-insensitive):
    /// "Nominatim" (default, no key required), "GeoNames", "Bing".
    /// </summary>
    public string Provider { get; set; } = "Nominatim";

    public NominatimOptions Nominatim { get; set; } = new();
    public GeoNamesOptions GeoNames { get; set; } = new();
    public BingMapsOptions BingMaps { get; set; } = new();
}

/// <summary>Options for the Nominatim (OpenStreetMap) geocoding adapter.</summary>
public sealed class NominatimOptions
{
    /// <summary>Base URL of the Nominatim instance. Defaults to the public OSM server.</summary>
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org/";

    /// <summary>
    /// Required by the Nominatim usage policy for the public instance.
    /// Set to something that identifies your application and provides contact information.
    /// </summary>
    public string UserAgent { get; set; } = "FastGeography/1.0 (https://github.com/FastGeography)";
}

/// <summary>Options for the GeoNames geocoding adapter.</summary>
public sealed class GeoNamesOptions
{
    /// <summary>Base URL of the GeoNames API. Defaults to the public server.</summary>
    public string BaseUrl { get; set; } = "http://api.geonames.org/";

    /// <summary>
    /// Free GeoNames username. Register at https://www.geonames.org/login .
    /// Leave empty to skip this provider.
    /// </summary>
    public string Username { get; set; } = string.Empty;
}

/// <summary>Options for the Bing Maps geocoding adapter.</summary>
public sealed class BingMapsOptions
{
    /// <summary>
    /// Bing Maps API key. Set via User Secrets or the GEOCODING__BINGMAPS__APIKEY
    /// environment variable. Never commit a real key to source control.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
