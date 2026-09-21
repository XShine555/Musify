using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Musify.Infrastructure.Persistence.Migrations
{

    public partial class AddUserAlbumExternalId : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserAlbums_OwnerUserId",
                table: "UserAlbums");

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "UserAlbums",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAlbums_OwnerUserId_ExternalId",
                table: "UserAlbums",
                columns: new[] { "OwnerUserId", "ExternalId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserAlbums_OwnerUserId_ExternalId",
                table: "UserAlbums");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "UserAlbums");

            migrationBuilder.CreateIndex(
                name: "IX_UserAlbums_OwnerUserId",
                table: "UserAlbums",
                column: "OwnerUserId");
        }
    }
}
