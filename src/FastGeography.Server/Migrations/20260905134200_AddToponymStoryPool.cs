using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastGeography.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddToponymStoryPool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ToponymStories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Body = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    Angle = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToponymStories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ToponymStories_NormalizedName_Category_LanguageCode",
                table: "ToponymStories",
                columns: new[] { "NormalizedName", "Category", "LanguageCode" });

            migrationBuilder.Sql(
                """
                INSERT INTO "ToponymStories" ("Id", "NormalizedName", "Category", "LanguageCode", "Body", "Angle", "CreatedAtUtc")
                SELECT gen_random_uuid(), "NormalizedName", "Category", "LanguageCode", LEFT("Story", 600), NULL, (NOW() AT TIME ZONE 'utc')
                FROM "Toponyms"
                WHERE "Story" IS NOT NULL AND "Story" <> '';
                """);

            migrationBuilder.DropColumn(
                name: "Story",
                table: "Toponyms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Story",
                table: "Toponyms",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Toponyms" t
                SET "Story" = s."Body"
                FROM (
                    SELECT DISTINCT ON ("NormalizedName", "Category", "LanguageCode")
                        "NormalizedName", "Category", "LanguageCode", "Body"
                    FROM "ToponymStories"
                    ORDER BY "NormalizedName", "Category", "LanguageCode", "CreatedAtUtc"
                ) s
                WHERE t."NormalizedName" = s."NormalizedName"
                  AND t."Category" = s."Category"
                  AND t."LanguageCode" = s."LanguageCode";
                """);

            migrationBuilder.DropTable(
                name: "ToponymStories");
        }
    }
}
