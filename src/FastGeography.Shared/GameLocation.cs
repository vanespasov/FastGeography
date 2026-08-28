namespace FastGeography.Shared
{
    using System;

    public class GameLocation
    {
        public LocationType LocationType { get; set; }
        public string? Answer { get; set; }
        public int Points { get; set; }
        public string? Coordinates { get; set; }
        public Uri? MapsUri => Coordinates == null ? null : BuildOsmUri(Coordinates);

        private static Uri BuildOsmUri(string coordinates)
        {
            var parts = coordinates.Split(',');
            if (parts.Length != 2) return new Uri($"https://www.openstreetmap.org/search?query={Uri.EscapeDataString(coordinates)}");
            var lat = parts[0].Trim();
            var lon = parts[1].Trim();
            return new Uri($"https://www.openstreetmap.org/?mlat={lat}&mlon={lon}#map=15/{lat}/{lon}");
        }
        public string? Description { get; set; }
    }
}
