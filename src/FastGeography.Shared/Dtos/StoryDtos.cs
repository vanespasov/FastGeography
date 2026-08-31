namespace FastGeography.Shared.Dtos;

/// <summary>
/// A single place for which a destination story is requested.
/// </summary>
public record StoryRequest(string Name, LocationType Type, string? Coordinates, string Lang);

/// <summary>
/// Body sent to POST api/destination-stories.
/// </summary>
public record DestinationStoriesRequest(List<StoryRequest> Places);

/// <summary>
/// A story result returned by the API.
/// </summary>
public record StoryResult(string Name, LocationType Type, string Story);

/// <summary>
/// Response from POST api/destination-stories.
/// </summary>
public record DestinationStoriesResponse(List<StoryResult> Stories);
