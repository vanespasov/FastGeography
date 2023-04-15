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
        public string? City { get; set; }
        public string? Village { get; set; }
        public string? State { get; set; }
        public string? Mountain { get; set; }
        public string? River { get; set; }
        public int CityPoints
        {
            get { return PointsPerTerm.GetValueOrDefault("city"); }
            set { SetPoints("city", value); }
        }
        
        public int VillagePoints
        {
            get { return PointsPerTerm.GetValueOrDefault("vilage"); }
            set { SetPoints("vilage", value); }
        }
        public int StatePoints
        {
            get { return PointsPerTerm.GetValueOrDefault("state"); }
            set { SetPoints("state", value); }
        }
        public int MountainPoints
        {
            get { return PointsPerTerm.GetValueOrDefault("mountain"); }
            set { SetPoints("mountain", value); }
        }
        public int RiverPoints
        {
            get { return PointsPerTerm.GetValueOrDefault("river"); }
            set { SetPoints("river", value); }
        }

        public int SecondsPlayed { get; set; }

        public Dictionary<string, int> PointsPerTerm { get; set; } = new Dictionary<string, int>();

        public int TotalPoints => PointsPerTerm.Sum(x => x.Value);

        private void SetPoints(string key, int value)
        {
            if (PointsPerTerm.ContainsKey(key)) { PointsPerTerm[key] = value; }
            else { PointsPerTerm.Add(key, value); }
        }

        public string SetCssClass(int points)
        {
            return points == 0 ? "table-light" : points > 0 ? "table-success" : "table-danger";
        }
    }
}
