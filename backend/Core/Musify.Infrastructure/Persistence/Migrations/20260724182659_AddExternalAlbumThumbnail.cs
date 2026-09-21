using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Musify.Infrastructure.Persistence.Migrations
{

    public partial class AddExternalAlbumThumbnail : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "ExternalAlbums",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "ExternalAlbums");
        }
    }
}
