using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Musify.Infrastructure.Persistence.Migrations
{

    public partial class RenameTimestampsAndAudit : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedDate",
                table: "Tracks",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Tracks",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedDate",
                table: "PlayLists",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "PlayLists",
                newName: "CreatedAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Tracks",
                newName: "UpdatedDate");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Tracks",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "PlayLists",
                newName: "UpdatedDate");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "PlayLists",
                newName: "CreatedDate");
        }
    }
}
