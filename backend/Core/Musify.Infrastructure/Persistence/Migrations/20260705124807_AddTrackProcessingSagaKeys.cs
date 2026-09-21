using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Musify.Infrastructure.Persistence.Migrations
{

    public partial class AddTrackProcessingSagaKeys : Migration
    {

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
