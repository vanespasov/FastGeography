using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastGeography.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddLanguageSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Toponyms_NormalizedName_Category",
                table: "Toponyms");

            migrationBuilder.AddColumn<string>(
                name: "LanguageCode",
                table: "Toponyms",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "en");

            migrationBuilder.AddColumn<string>(
                name: "LanguageCode",
                table: "GameRounds",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Toponyms_NormalizedName_Category_LanguageCode",
                table: "Toponyms",
                columns: new[] { "NormalizedName", "Category", "LanguageCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Toponyms_NormalizedName_Category_LanguageCode",
                table: "Toponyms");

            migrationBuilder.DropColumn(
                name: "LanguageCode",
                table: "Toponyms");

            migrationBuilder.DropColumn(
                name: "LanguageCode",
                table: "GameRounds");

            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_Toponyms_NormalizedName_Category",
                table: "Toponyms",
                columns: new[] { "NormalizedName", "Category" },
                unique: true);
        }
    }
}
