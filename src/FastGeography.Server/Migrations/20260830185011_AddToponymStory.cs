using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastGeography.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddToponymStory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Story",
                table: "Toponyms",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Story",
                table: "Toponyms");
        }
    }
}
