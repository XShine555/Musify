using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Musify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackSourceArtistExternalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Artist",
                table: "Tracks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Tracks",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Tracks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_Source_ExternalId",
                table: "Tracks",
                columns: new[] { "Source", "ExternalId" },
                unique: true,
                filter: "\"ExternalId\" <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tracks_Source_ExternalId",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "Artist",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Tracks");
        }
    }
}
