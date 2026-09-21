using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Musify.Infrastructure.Persistence.Migrations
{

    public partial class AddPlayListVisibility : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Visibility",
                table: "PlayLists",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "PlayLists");
        }
    }
}
