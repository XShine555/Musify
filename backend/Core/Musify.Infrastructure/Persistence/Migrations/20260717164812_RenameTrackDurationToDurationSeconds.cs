using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Musify.Infrastructure.Persistence.Migrations
{

    public partial class RenameTrackDurationToDurationSeconds : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Duration",
                table: "Tracks",
                newName: "DurationSeconds");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DurationSeconds",
                table: "Tracks",
                newName: "Duration");
        }
    }
}
