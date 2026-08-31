namespace FastGeography.Server.Services;

using FastGeography.Server.Data;
using FastGeography.Server.Options;
using FastGeography.Server.Services.Ai;
using FastGeography.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

/// <summary>
/// Generates short destination stories via the configured AI provider.
/// Results are cached in <see cref="FastGeography.Server.Data.Entities.Toponym.Story"/>.
/// </summary>
public sealed class DestinationStoryService : IDestinationStoryService
{
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

        await using (var readScope = _scopeFactory.CreateAsyncScope())
        {
            var db = readScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var entry = await db.Toponyms
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.NormalizedName == normalized
                         && t.Category == type
                         && t.LanguageCode == langCode,
                    ct);

            if (entry?.Story is { Length: > 0 } cached)
            {
                _logger.LogDebug("Story cache hit for {Place}/{Type}/{Lang}", place, type, langCode);
                return cached;
            }
        }

        var countryOrRegion = coordinates is not null
            ? $"unknown; coordinates {coordinates}"
            : "unknown location";

        var language = lang == GameLanguage.Mk ? "Macedonian" : "English";
        var userPrompt = DestinationStoryPrompt.BuildUserPrompt(
            place, type.ToString(), countryOrRegion, language);

        var story = await _chat.CompleteAsync(DestinationStoryPrompt.System, userPrompt, ct);
        if (string.IsNullOrWhiteSpace(story) || IsRefusal(story))
        {
            _logger.LogWarning("AI returned empty or refusal for {Place}/{Type}", place, type);
            return null;
        }

        if (story.Length > MaxStoryChars)
            story = story[..MaxStoryChars].TrimEnd() + "…";

        await TrySaveStoryAsync(normalized, type, langCode, story, ct);
        return story;
    }

    private async Task TrySaveStoryAsync(
        string normalized,
        LocationType type,
        string langCode,
        string story,
        CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var entry = await db.Toponyms
                .FirstOrDefaultAsync(
                    t => t.NormalizedName == normalized
                         && t.Category == type
                         && t.LanguageCode == langCode,
                    ct);

            if (entry is not null)
            {
                entry.Story = story;
                await db.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not persist story for {Normalized}/{Type}/{Lang}", normalized, type, langCode);
        }
    }

    private static bool IsRefusal(string text)
    {
        var lower = text.ToLowerInvariant();
        return lower.StartsWith("i'm sorry") || lower.StartsWith("i cannot") || lower.StartsWith("i can't");
    }
}
