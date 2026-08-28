namespace FastGeography.Server.Controllers;

using System.Security.Claims;

using FastGeography.Server.Data;
using FastGeography.Server.Data.Entities;
using FastGeography.Server.Services;
using FastGeography.Shared;
using FastGeography.Shared.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/games")]
[Authorize]
public class GamesController : ControllerBase
{
    private static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(5);

    private readonly ApplicationDbContext _db;
    private readonly IGeocodingService _geocodingService;

    public GamesController(ApplicationDbContext db, IGeocodingService geocodingService)
    {
        _db = db;
        _geocodingService = geocodingService;
    }

    [HttpPost("solo/start")]
    public async Task<IActionResult> StartSolo()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var letter = (char)('A' + Random.Shared.Next(0, 26));
        var now = DateTime.UtcNow;

        var round = new GameRound
        {
            Id = Guid.NewGuid(),
            Mode = GameMode.Solo,
            Letter = letter,
            StartedAt = now,
            EndsAt = now.AddSeconds(ScoringRules.DefaultTimerSeconds)
        };

        _db.GameRounds.Add(round);
        await _db.SaveChangesAsync();

        return Ok(new SoloStartResponse(round.Id, round.Letter, round.EndsAt));
    }

    [HttpPost("solo/{roundId:guid}/submit")]
    public async Task<IActionResult> SubmitSolo(Guid roundId, [FromBody] SubmitAnswersRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var round = await _db.GameRounds.FindAsync(roundId);
        if (round is null) return NotFound();
        if (round.Mode != GameMode.Solo) return BadRequest("Round is not a solo round.");
        if (DateTime.UtcNow > round.EndsAt + GracePeriod) return BadRequest("Submission deadline passed.");

        var alreadySubmitted = await _db.RoundSubmissions
            .AnyAsync(s => s.RoundId == roundId && s.UserId == userId);
        if (alreadySubmitted) return Conflict("Already submitted for this round.");

        var details = await ValidateAllAsync(round.Letter, request);
        var submission = BuildSubmission(roundId, userId, request, details);

        _db.RoundSubmissions.Add(submission);

        var profile = await _db.PlayerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile is not null)
        {
            profile.CareerPoints += submission.CityPoints + submission.VillagePoints +
                                    submission.CountryPoints + submission.RiverPoints +
                                    submission.MountainPoints;
            profile.GamesPlayed++;
        }

        await _db.SaveChangesAsync();

        var badge = BadgeCalculator.Calculate(profile?.CareerPoints ?? 0).ToString();
        return Ok(new SoloSubmitResponse(submission.TotalPoints, badge, details));
    }

    private async Task<List<LocationResult>> ValidateAllAsync(char letter, SubmitAnswersRequest req)
    {
        var tasks = new[]
        {
            ValidateAsync(LocationType.City,     req.City,     letter),
            ValidateAsync(LocationType.Village,  req.Village,  letter),
            ValidateAsync(LocationType.Country,  req.Country,  letter),
            ValidateAsync(LocationType.River,    req.River,    letter),
            ValidateAsync(LocationType.Mountain, req.Mountain, letter),
        };

        await Task.WhenAll(tasks);
        return tasks.Select(t => t.Result).ToList();
    }

    private async Task<LocationResult> ValidateAsync(LocationType type, string? answer, char letter)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return new LocationResult(type, answer, ScoringRules.EmptyPoints, null);

        if (!answer.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase))
            return new LocationResult(type, answer, ScoringRules.WrongLetterPoints, null);

        var result = await _geocodingService.ValidateAsync(answer, type);
        return new LocationResult(type, answer, result.Points, result.Coordinates);
    }

    private static RoundSubmission BuildSubmission(
        Guid roundId, string userId, SubmitAnswersRequest req, List<LocationResult> details)
    {
        int PointsFor(LocationType t) => details.First(d => d.Type == t).Points;

        return new RoundSubmission
        {
            Id = Guid.NewGuid(),
            RoundId = roundId,
            UserId = userId,
            CityAnswer = req.City,
            VillageAnswer = req.Village,
            CountryAnswer = req.Country,
            RiverAnswer = req.River,
            MountainAnswer = req.Mountain,
            CityPoints = PointsFor(LocationType.City),
            VillagePoints = PointsFor(LocationType.Village),
            CountryPoints = PointsFor(LocationType.Country),
            RiverPoints = PointsFor(LocationType.River),
            MountainPoints = PointsFor(LocationType.Mountain),
            RankInRound = 1
        };
    }
}
