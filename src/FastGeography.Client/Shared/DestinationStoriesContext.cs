namespace FastGeography.Client.Shared;

using FastGeography.Shared;

/// <summary>
/// A loaded destination story with optional place image.
/// </summary>
public sealed record DestinationStoryEntry(
    string Story,
    string? ImageUrl,
    string? ImageAttribution);

/// <summary>
/// Cascading context for destination story lookup inside a table.
/// </summary>
public sealed class DestinationStoriesContext
{
    private readonly Dictionary<(string NormalizedName, LocationType Type), DestinationStoryEntry> _stories;
    private readonly HashSet<(string NormalizedName, LocationType Type)> _pendingKeys;

    public DestinationStoriesContext(
        IReadOnlyList<(string Name, LocationType Type, string Story, string? ImageUrl, string? ImageAttribution)>? stories,
        IReadOnlyList<(string Name, LocationType Type)>? pendingKeys = null)
    {
        _stories = new Dictionary<(string, LocationType), DestinationStoryEntry>();
        _pendingKeys = new HashSet<(string, LocationType)>();

        if (stories is not null)
        {
            foreach (var (name, type, story, imageUrl, imageAttribution) in stories)
            {
                _stories[NormalizeKey(name, type)] = new DestinationStoryEntry(
                    story, imageUrl, imageAttribution);
            }
        }

        if (pendingKeys is not null)
        {
            foreach (var (name, type) in pendingKeys)
                _pendingKeys.Add(NormalizeKey(name, type));
        }
    }

    public bool IsEligible(GameLocation? location) =>
        location is not null
        && location.Points == ScoringRules.ValidPoints
        && !string.IsNullOrWhiteSpace(location.Answer);

    public bool IsStoryPending(GameLocation? location)
    {
        if (!IsEligible(location))
            return false;

        var key = NormalizeKey(location!.Answer!, location.LocationType);
        return _pendingKeys.Contains(key) && !_stories.ContainsKey(key);
    }

    public DestinationStoryEntry? GetStory(GameLocation? location)
    {
        if (!IsEligible(location))
            return null;

        var key = NormalizeKey(location!.Answer!, location.LocationType);
        return _stories.TryGetValue(key, out var story) ? story : null;
    }

    public bool ShouldShowStorySlot(GameLocation? location) =>
        IsStoryPending(location) || GetStory(location) is not null;

    private static (string NormalizedName, LocationType Type) NormalizeKey(string name, LocationType type) =>
        (name.Trim().ToLowerInvariant(), type);
}
