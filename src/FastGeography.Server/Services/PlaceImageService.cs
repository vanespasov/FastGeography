namespace FastGeography.Server.Services;

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using FastGeography.Server.Data;
using FastGeography.Server.Options;
using FastGeography.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

/// <summary>
/// Looks up a place thumbnail from Wikipedia (by name) or Wikimedia Commons (by coordinates),
/// caching results on matching <see cref="Data.Entities.Toponym"/> rows.
/// </summary>
public sealed class PlaceImageService : IPlaceImageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PlaceImageOptions _options;
    private readonly ILogger<PlaceImageService> _logger;

    public PlaceImageService(
        ApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        IOptions<PlaceImageOptions> options,
        ILogger<PlaceImageService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PlaceImageResult?> GetImageAsync(
        string normalizedName,
        LocationType type,
        string displayName,
        double? latitude,
        double? longitude,
        GameLanguage lang,
        CancellationToken ct = default)
    {
        var cached = await TryGetCachedAsync(normalizedName, type, ct);
        if (cached.ShouldFetch)
        {
            var result = await FetchImageAsync(displayName, latitude, longitude, lang, ct);
            await PersistCacheAsync(normalizedName, type, result, ct);
            return result;
        }

        return cached.Image;
    }

    private async Task<CacheLookup> TryGetCachedAsync(
        string normalizedName,
        LocationType type,
        CancellationToken ct)
    {
        var row = await _db.Toponyms
            .AsNoTracking()
            .Where(t => t.NormalizedName == normalizedName && t.Category == type && t.ImageFetchedAtUtc != null)
            .OrderByDescending(t => t.ImageFetchedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (row is null)
            return CacheLookup.Fetch();

        if (!string.IsNullOrWhiteSpace(row.ImageUrl))
        {
            return CacheLookup.Hit(new PlaceImageResult(
                row.ImageUrl,
                row.ImageAttribution ?? "Wikipedia"));
        }

        var retryAfter = row.ImageFetchedAtUtc!.Value.AddDays(_options.MissRetryDays);
        return DateTime.UtcNow < retryAfter
            ? CacheLookup.Miss()
            : CacheLookup.Fetch();
    }

    private readonly record struct CacheLookup(PlaceImageResult? Image, bool ShouldFetch)
    {
        public static CacheLookup Hit(PlaceImageResult image) => new(image, false);
        public static CacheLookup Miss() => new(null, false);
        public static CacheLookup Fetch() => new(null, true);
    }

    private async Task<PlaceImageResult?> FetchImageAsync(
        string displayName,
        double? latitude,
        double? longitude,
        GameLanguage lang,
        CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("wikimedia");

        var langs = lang == GameLanguage.En
            ? new[] { "en" }
            : new[] { "mk", "en" };

        foreach (var wikiLang in langs)
        {
            var wiki = await TryWikipediaSummaryAsync(client, wikiLang, displayName, ct);
            if (wiki is not null)
                return wiki;
        }

        if (latitude is not null && longitude is not null)
        {
            var commons = await TryCommonsGeosearchAsync(client, latitude.Value, longitude.Value, ct);
            if (commons is not null)
                return commons;
        }

        return null;
    }

    internal static async Task<PlaceImageResult?> TryWikipediaSummaryAsync(
        HttpClient client,
        string wikiLang,
        string displayName,
        CancellationToken ct)
    {
        var title = displayName.Trim().Replace(' ', '_');
        var url = $"https://{wikiLang}.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(title)}";

        using var response = await client.GetAsync(url, ct);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
            return null;

        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<WikipediaSummary>(JsonOptions, ct);
        var source = summary?.Thumbnail?.Source;
        if (string.IsNullOrWhiteSpace(source))
            return null;

        return new PlaceImageResult(source, "Wikipedia");
    }

    internal static async Task<PlaceImageResult?> TryCommonsGeosearchAsync(
        HttpClient client,
        double latitude,
        double longitude,
        CancellationToken ct)
    {
        var url =
            "https://commons.wikimedia.org/w/api.php" +
            $"?action=query&generator=geosearch&gscoord={latitude}|{longitude}" +
            "&gsradius=10000&gslimit=1&prop=pageimages&piprop=thumbnail&pithumbsize=400&format=json";

        using var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CommonsQueryResponse>(JsonOptions, ct);
        var page = payload?.Query?.Pages?.Values.FirstOrDefault();
        var source = page?.Thumbnail?.Source;
        if (string.IsNullOrWhiteSpace(source))
            return null;

        return new PlaceImageResult(source, "Wikimedia Commons");
    }

    private async Task PersistCacheAsync(
        string normalizedName,
        LocationType type,
        PlaceImageResult? result,
        CancellationToken ct)
    {
        var rows = await _db.Toponyms
            .Where(t => t.NormalizedName == normalizedName && t.Category == type)
            .ToListAsync(ct);

        if (rows.Count == 0)
            return;

        var now = DateTime.UtcNow;
        foreach (var row in rows)
        {
            row.ImageUrl = result?.ImageUrl;
            row.ImageAttribution = result?.Attribution;
            row.ImageFetchedAtUtc = now;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not persist place image for {Name}/{Type}", normalizedName, type);
        }
    }

    private sealed class WikipediaSummary
    {
        [JsonPropertyName("thumbnail")]
        public WikipediaThumbnail? Thumbnail { get; set; }
    }

    private sealed class WikipediaThumbnail
    {
        [JsonPropertyName("source")]
        public string? Source { get; set; }
    }

    private sealed class CommonsQueryResponse
    {
        [JsonPropertyName("query")]
        public CommonsQuery? Query { get; set; }
    }

    private sealed class CommonsQuery
    {
        [JsonPropertyName("pages")]
        public Dictionary<string, CommonsPage>? Pages { get; set; }
    }

    private sealed class CommonsPage
    {
        [JsonPropertyName("thumbnail")]
        public WikipediaThumbnail? Thumbnail { get; set; }
    }
}
