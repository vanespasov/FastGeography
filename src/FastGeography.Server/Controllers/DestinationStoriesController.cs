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
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DestinationStoriesController> _logger;

    public DestinationStoriesController(
        IDestinationStoryService stories,
        ApplicationDbContext db,
        ILogger<DestinationStoriesController> logger)
    {
        _stories = stories;
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

            var lang = GameLanguageExtensions.Parse(place.Lang);
            var normalized = place.Name.Trim().ToLowerInvariant();

            // Safety check: only generate stories for places that were already verified
            // by the geocoding pipeline (a Toponym row exists).
            var exists = await _db.Toponyms
                .AnyAsync(
                    t => t.NormalizedName == normalized
                         && t.Category == place.Type
                         && t.LanguageCode == lang.ToCode(),
                    ct);

            if (!exists)
            {
                _logger.LogDebug(
                    "Skipping story for unverified place '{Name}' ({Type}/{Lang})",
                    place.Name, place.Type, place.Lang);
                continue;
            }

            var story = await _stories.GetStoryAsync(place.Name, place.Type, place.Coordinates, lang, ct);
            if (story is not null)
                results.Add(new StoryResult(place.Name, place.Type, story));
        }

        return Ok(new DestinationStoriesResponse(results));
    }
}
