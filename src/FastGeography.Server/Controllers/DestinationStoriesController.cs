namespace FastGeography.Server.Controllers;

using FastGeography.Server.Data;
using FastGeography.Server.Services;
using FastGeography.Shared;
using FastGeography.Shared.Dtos;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

[ApiController]
[EnableRateLimiting("stories")]
public sealed class DestinationStoriesController : ControllerBase
{
    private const int MaxPlacesPerRequest = 10;

    private readonly IDestinationStoryService _stories;
    private readonly IPlaceImageService _images;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DestinationStoriesController> _logger;

    public DestinationStoriesController(
        IDestinationStoryService stories,
        IPlaceImageService images,
        ApplicationDbContext db,
        ILogger<DestinationStoriesController> logger)
    {
        _stories = stories;
        _images = images;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Generates (or retrieves cached) short travel stories for the supplied places.
    /// Only places that already exist in the Toponyms table are processed — this
    /// prevents clients from injecting arbitrary prompts for unverified locations.
    /// </summary>
    [HttpPost("api/destination-stories")]
    public async Task<IActionResult> GetStories(
        [FromBody] DestinationStoriesRequest request,
        CancellationToken ct)
    {
        if (request.Places is null || request.Places.Count == 0)
            return BadRequest("No places supplied.");

        if (request.Places.Count > MaxPlacesPerRequest)
            return BadRequest($"Maximum {MaxPlacesPerRequest} places per request.");

        var results = new List<StoryResult>();

        foreach (var place in request.Places)
        {
            if (string.IsNullOrWhiteSpace(place.Name) || place.Name.Length > ScoringRules.MaxAnswerLength)
                continue;

            // Story language follows the UI picker (place.Lang), which may differ
            // from the language the answer was verified in.
            var lang = GameLanguageExtensions.Parse(place.Lang);
            var normalized = place.Name.Trim().ToLowerInvariant();

            // Safety check: only generate stories for places that were already verified
            // by the geocoding pipeline (a Toponym row exists in any language).
            var exists = await _db.Toponyms
                .AnyAsync(
                    t => t.NormalizedName == normalized
                         && t.Category == place.Type,
                    ct);

            if (!exists)
            {
                _logger.LogDebug(
                    "Skipping story for unverified place '{Name}' ({Type}/{Lang})",
                    place.Name, place.Type, place.Lang);
                continue;
            }

            var story = await _stories.GetStoryAsync(place.Name, place.Type, place.Coordinates, lang, ct);
            if (story is null)
                continue;

            var (lat, lon) = ParseCoordinates(place.Coordinates);
            if (lat is null || lon is null)
            {
                var coords = await _db.Toponyms
                    .AsNoTracking()
                    .Where(t => t.NormalizedName == normalized && t.Category == place.Type)
                    .Select(t => new { t.Latitude, t.Longitude })
                    .FirstOrDefaultAsync(ct);
                if (coords is not null)
                {
                    lat = coords.Latitude;
                    lon = coords.Longitude;
                }
            }

            var image = await _images.GetImageAsync(
                normalized, place.Type, place.Name, lat, lon, lang, ct);

            results.Add(new StoryResult(
                place.Name,
                place.Type,
                story,
                image?.ImageUrl,
                image?.Attribution));
        }

        return Ok(new DestinationStoriesResponse(results));
    }

    private static (double? Lat, double? Lon) ParseCoordinates(string? coordinates)
    {
        if (string.IsNullOrWhiteSpace(coordinates))
            return (null, null);

        var parts = coordinates.Split(',');
        if (parts.Length != 2)
            return (null, null);

        if (double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var lat)
            && double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var lon))
            return (lat, lon);

        return (null, null);
    }
}
