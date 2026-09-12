using System.Globalization;
using System.Text;

using FastGeography.Server.Data.Seed;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastGeography.Server.Migrations
{
    /// <inheritdoc />
    public partial class SeedWellKnownToponyms : Migration
    {
        private const string VerifiedAt = "2026-01-01 00:00:00+00";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var records = WellKnownToponyms.All;
            const int BatchSize = 50;

            for (int offset = 0; offset < records.Count; offset += BatchSize)
            {
                var batch = records.Skip(offset).Take(BatchSize).ToList();
                var sb = new StringBuilder();
                sb.AppendLine("INSERT INTO \"Toponyms\" (\"Id\",\"NormalizedName\",\"DisplayName\",\"Category\",\"LanguageCode\",\"Latitude\",\"Longitude\",\"Provider\",\"VerifiedAtUtc\")");
                sb.AppendLine("VALUES");

                for (int i = 0; i < batch.Count; i++)
                {
                    var r    = batch[i];
                    var id   = r.Id.ToString().ToLower();
                    var norm = r.NormalizedName.Replace("'", "''");
                    var disp = r.DisplayName.Replace("'", "''");
                    var lat  = r.Latitude.ToString(CultureInfo.InvariantCulture);
                    var lon  = r.Longitude.ToString(CultureInfo.InvariantCulture);
                    var comma = i < batch.Count - 1 ? "," : "";
                    sb.AppendLine(
                        $"  ('{id}','{norm}','{disp}',{(int)r.Category},'{r.LanguageCode}',{lat},{lon},'Seed','{VerifiedAt}'){comma}");
                }

                sb.AppendLine("ON CONFLICT (\"NormalizedName\",\"Category\",\"LanguageCode\") DO NOTHING;");
                migrationBuilder.Sql(sb.ToString());
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"Toponyms\" WHERE \"Provider\" = 'Seed';");
        }
    }
}
