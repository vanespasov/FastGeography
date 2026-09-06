namespace FastGeography.Client.Shared;

using FastGeography.Shared;

public static class PlaceCategoryIcon
{
    public static string CategoryIconPath(LocationType type) => type switch
    {
        LocationType.City => "images/place-city.svg",
        LocationType.Village => "images/place-village.svg",
        LocationType.Country => "images/place-country.svg",
        LocationType.River => "images/place-river.svg",
        LocationType.Mountain => "images/place-mountain.svg",
        _ => "images/place-city.svg"
    };
}
