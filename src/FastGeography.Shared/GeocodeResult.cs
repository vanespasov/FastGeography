namespace FastGeography.Shared;

public sealed class GeocodeResult
{
    public LocationType LocationType { get; init; }
    public int Points { get; init; }
    public string? Coordinates { get; init; }
}
