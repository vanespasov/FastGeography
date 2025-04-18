namespace FastGeography.Shared
{
    using System;

    public class GameLocation
    {
        public LocationType LocationType { get; set; }
        public string? Answer { get; set; }
        public int Points { get; set; }
        public string? Coordinates { get; set; }
        public Uri? MapsUri => Coordinates == null ? null : new Uri($"https://www.bing.com/maps?cp={Coordinates.Replace(',','~')}&lvl=15");
        public string? Description { get; set; }
    }
}
