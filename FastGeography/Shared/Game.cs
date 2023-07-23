namespace FastGeography.Shared
{
    using System.Collections.Generic;
    using System.Linq;

    public class Game
    {
        public Guid Id { get; set; }
        public DateTime DatePlayed { get; set; }
        public bool IsFinished { get; set; }
        public char Letter { get; set; }
        public GameLocation? City { get; set; }
        public GameLocation? Village { get; set; }
        public GameLocation? Country { get; set; }
        public GameLocation? Mountain { get; set; }
        public GameLocation? River { get; set; }

        public int SecondsPlayed { get; set; }

        public Dictionary<LocationType, int> PointsPerTerm { get; set; } = new Dictionary<LocationType, int>();

        public int TotalPoints => City.Points + Village.Points + Country.Points + Mountain.Points + River.Points;

        private void SetPoints(LocationType key, int value)
        {
            if (PointsPerTerm.ContainsKey(key))
            {
                PointsPerTerm[key] = value;
            }
            else
            {
                PointsPerTerm.Add(key, value);
            }
        }

        public string SetCssClass(int points)
        {
            return points == 0 ? "table-light" : points > 0 ? "table-success" : "table-danger";
        }
    }
}
