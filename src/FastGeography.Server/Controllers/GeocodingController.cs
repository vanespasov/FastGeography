namespace FastGeography.Server.Controllers;

using FastGeography.Server.Services;
using FastGeography.Shared;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[EnableRateLimiting("geocode")]
public class GeocodingController : ControllerBase
{
    private readonly IGeocodingService _geocoding;
    private readonly ILogger<GeocodingController> _logger;

    public GeocodingController(IGeocodingService geocoding, ILogger<GeocodingController> logger)
    {
        _geocoding = geocoding;
        _logger = logger;
    }

    /// <summary>
    /// Validates a player's geography answer for the given location type.
    /// Location is limited to 100 characters to prevent quota abuse.
    /// Returns a <see cref="GeocodeResult"/> with the awarded points and coordinates.
    /// The "/bingmaps" path is kept for backward compatibility with existing clients.
    /// </summary>
    [HttpGet("geocode/{location}/{locationType}")]
    [HttpGet("bingmaps/{location}/{locationType}")]
    public async Task<IActionResult> GetLocationType(
        string location,
        LocationType locationType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(location) || location.Length > ScoringRules.MaxAnswerLength)
            return BadRequest("Location must be between 1 and 100 characters.");

        _logger.LogInformation(
            "Validating answer '{Location}' for type {LocationType}", location, locationType);

        var result = await _geocoding.ValidateAsync(location, locationType, cancellationToken);
        return Ok(result);
    }
}
