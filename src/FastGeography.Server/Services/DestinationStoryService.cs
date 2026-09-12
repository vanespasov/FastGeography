namespace FastGeography.Server.Services;

using FastGeography.Server.Data;
using FastGeography.Server.Data.Entities;
using FastGeography.Server.Services.Ai;
using FastGeography.Shared;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Generates short destination stories via the configured AI provider.
/// A pool of up to <see cref="TargetPoolSize"/> stories is stored in
/// <see cref="ToponymStory"/> and grown lazily (one new story per request).
/// </summary>
public sealed class DestinationStoryService : IDestinationStoryService
{
    internal const int TargetPoolSize = 5;
    private const int MaxStoryChars = 500;

    private readonly IChatCompletionClient _chat;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DestinationStoryService> _logger;

    public DestinationStoryService(
        IChatCompletionClient chat,
        IServiceScopeFactory scopeFactory,
        ILogger<DestinationStoryService> logger)
    {
        _chat = chat;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<string?> GetStoryAsync(
        string place,
        LocationType type,
        string? coordinates,
        GameLanguage lang,
        CancellationToken ct = default)
    {
        var langCode = lang.ToCode();
        var normalized = place.Trim().ToLowerInvariant();

        var pool = await LoadPoolAsync(normalized, type, langCode, ct);

        if (pool.Count >= TargetPoolSize)
        {
            _logger.LogDebug(
                "Story pool full for {Place}/{Type}/{Lang} ({Count})",
                place, type, langCode, pool.Count);
            return PickRandom(pool).Body;
        }

        var countryOrRegion = coordinates is not null
            ? $"unknown; coordinates {coordinates}"
            : "unknown location";

        var angle = PickUnusedAngle(pool);
        var existingBodies = pool.Select(s => s.Body).ToList();
        var userPrompt = DestinationStoryPrompt.BuildUserPrompt(
            place, type, countryOrRegion, lang, angle, existingBodies);

        var story = await _chat.CompleteAsync(DestinationStoryPrompt.System(lang), userPrompt, ct);
        if (string.IsNullOrWhiteSpace(story) || IsRefusal(story))
        {
            _logger.LogWarning("AI returned empty or refusal for {Place}/{Type}", place, type);
            return pool.Count > 0 ? PickRandom(pool).Body : null;
        }

        if (story.Length > MaxStoryChars)
            story = story[..MaxStoryChars].TrimEnd() + "…";

        await TrySaveStoryAsync(normalized, type, langCode, story, angle.ToKey(), ct);
        return story;
    }

    private async Task<List<ToponymStory>> LoadPoolAsync(
        string normalized,
        LocationType type,
        string langCode,
        CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.ToponymStories
            .AsNoTracking()
            .Where(s => s.NormalizedName == normalized
                        && s.Category == type
                        && s.LanguageCode == langCode)
            .ToListAsync(ct);
    }

    private async Task TrySaveStoryAsync(
        string normalized,
        LocationType type,
        string langCode,
        string story,
        string angle,
        CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var count = await db.ToponymStories.CountAsync(
                s => s.NormalizedName == normalized
                     && s.Category == type
                     && s.LanguageCode == langCode,
                ct);

            if (count >= TargetPoolSize)
                return;

            db.ToponymStories.Add(new ToponymStory
            {
                Id = Guid.NewGuid(),
                NormalizedName = normalized,
                Category = type,
                LanguageCode = langCode,
                Body = story,
                Angle = angle,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not persist story for {Normalized}/{Type}/{Lang}", normalized, type, langCode);
        }
    }

    internal static StoryAngle PickUnusedAngle(IReadOnlyList<ToponymStory> pool)
    {
        var used = new HashSet<StoryAngle>();
        foreach (var row in pool)
        {
            if (DestinationStoryPrompt.TryParseAngle(row.Angle, out var parsed))
                used.Add(parsed);
        }

        var unused = DestinationStoryPrompt.AllAngles.Where(a => !used.Contains(a)).ToList();
        var choices = unused.Count > 0 ? unused : DestinationStoryPrompt.AllAngles.ToList();
        return choices[Random.Shared.Next(choices.Count)];
    }

    private static ToponymStory PickRandom(IReadOnlyList<ToponymStory> pool) =>
        pool[Random.Shared.Next(pool.Count)];

    private static bool IsRefusal(string text)
    {
        var lower = text.ToLowerInvariant();
        return lower.StartsWith("i'm sorry") || lower.StartsWith("i cannot") || lower.StartsWith("i can't");
    }
}
