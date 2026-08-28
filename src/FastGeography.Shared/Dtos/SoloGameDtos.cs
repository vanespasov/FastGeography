namespace FastGeography.Shared.Dtos;

using FastGeography.Shared;

public record SoloStartResponse(Guid RoundId, char Letter, DateTime EndsAt);

public record SubmitAnswersRequest(
    string? City,
    string? Village,
    string? Country,
    string? River,
    string? Mountain);

public record LocationResult(LocationType Type, string? Answer, int Points, string? Coordinates);

public record SoloSubmitResponse(int TotalPoints, string Badge, List<LocationResult> Details);
