using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Musify.Infrastructure.Persistence.Migrations
{

    public partial class AddTrackDuration : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Duration",
                table: "Tracks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Tracks");
        }
    }
}
