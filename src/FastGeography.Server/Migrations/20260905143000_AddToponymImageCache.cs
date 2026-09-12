using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FastGeography.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddToponymImageCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageAttribution",
                table: "Toponyms",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ImageFetchedAtUtc",
                table: "Toponyms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Toponyms",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageAttribution",
                table: "Toponyms");

            migrationBuilder.DropColumn(
                name: "ImageFetchedAtUtc",
                table: "Toponyms");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Toponyms");
        }
    }
}
