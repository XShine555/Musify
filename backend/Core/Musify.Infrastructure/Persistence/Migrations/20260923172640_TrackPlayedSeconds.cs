using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Musify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackPlayedSeconds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCounted",
                table: "ListeningHistory",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastProgressAt",
                table: "ListeningHistory",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PlayedSeconds",
                table: "ListeningHistory",
                type: "double precision",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"ListeningHistory\" SET \"IsCounted\" = TRUE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCounted",
                table: "ListeningHistory");

            migrationBuilder.DropColumn(
                name: "LastProgressAt",
                table: "ListeningHistory");

            migrationBuilder.DropColumn(
                name: "PlayedSeconds",
                table: "ListeningHistory");
        }
    }
}
