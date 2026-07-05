using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Musify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackProcessingSagaKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudioKey",
                table: "TrackProcessingState",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bucket",
                table: "TrackProcessingState",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PictureKey",
                table: "TrackProcessingState",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioKey",
                table: "TrackProcessingState");

            migrationBuilder.DropColumn(
                name: "Bucket",
                table: "TrackProcessingState");

            migrationBuilder.DropColumn(
                name: "PictureKey",
                table: "TrackProcessingState");
        }
    }
}
