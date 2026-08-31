namespace FastGeography.Server.Controllers;

using System.Security.Claims;

using FastGeography.Server.Data;
using FastGeography.Shared;
using FastGeography.Shared.Dtos;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/leaderboard")]
public class LeaderboardController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public LeaderboardController(ApplicationDbContext db) => _db = db;

    /// <summary>Returns top 25 players. filter=alltime (default) or week.</summary>
    [HttpGet]
    public async Task<IActionResult> GetLeaderboard([FromQuery] string filter = "alltime")
    {
        List<(string UserId, int Points, int Games)> raw;

        if (filter == "week")
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);

            // GroupBy with aggregates over a joined navigation cannot be translated by EF Core.
            // Fetch only the columns needed, then aggregate in memory.
            var submissions = await _db.RoundSubmissions
                .Where(s => s.Round.StartedAt >= cutoff)
                .Select(s => new
                {
                    s.UserId,
                    Points = s.CityPoints + s.VillagePoints + s.CountryPoints + s.RiverPoints + s.MountainPoints
                })
                .ToListAsync();

            raw = submissions
                .GroupBy(s => s.UserId)
                .Select(g => (g.Key, g.Sum(s => s.Points), g.Count()))
                .OrderByDescending(x => x.Item2)
                .Take(25)
                .ToList();
        }
        else
        {
            raw = await _db.PlayerProfiles
                .Select(p => new ValueTuple<string, int, int>(p.UserId, p.CareerPoints, p.GamesPlayed))
                .OrderByDescending(x => x.Item2)
                .Take(25)
                .ToListAsync();
        }

        var userIds = raw.Select(r => r.Item1).ToList();
        var displayNames = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

        var entries = raw.Select((r, i) => new LeaderboardEntry(
            Rank: i + 1,
            DisplayName: displayNames.TryGetValue(r.Item1, out var n) ? n : "Unknown",
            CareerPoints: r.Item2,
            Badge: BadgeCalculator.Calculate(r.Item2).ToString(),
            GamesPlayed: r.Item3)).ToList();

        return Ok(entries);
    }

    /// <summary>Returns current user's rank, career stats, and last 10 rounds.</summary>
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> GetMyStats()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var profile = await _db.PlayerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        var user = await _db.Users.FindAsync(userId);
        if (profile is null || user is null) return NotFound();

        var rank = await _db.PlayerProfiles
            .CountAsync(p => p.CareerPoints > profile.CareerPoints) + 1;

        var recent = await _db.RoundSubmissions
            .Include(s => s.Round)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.Round.StartedAt)
            .Take(10)
            .Select(s => new RecentRound(
                s.RoundId,
                s.Round.Mode.ToString(),
                s.Round.Letter,
                s.CityPoints + s.VillagePoints + s.CountryPoints + s.RiverPoints + s.MountainPoints,
                s.Round.StartedAt))
            .ToListAsync();

        return Ok(new PlayerStats(
            rank,
            user.DisplayName,
            profile.CareerPoints,
            BadgeCalculator.Calculate(profile.CareerPoints).ToString(),
            profile.GamesPlayed,
            recent));
    }
}
