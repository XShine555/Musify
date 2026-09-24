using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Musify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MixKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Mixes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Mixes used to store their Spanish title; the daily mix is kind 1, everything else is discovery.
            migrationBuilder.Sql("UPDATE \"Mixes\" SET \"Kind\" = 1 WHERE \"Title\" = 'Tu mezcla diaria';");

            migrationBuilder.DropColumn(
                name: "Subtitle",
                table: "Mixes");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Mixes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Mixes");

            migrationBuilder.AddColumn<string>(
                name: "Subtitle",
                table: "Mixes",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Mixes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
