using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Musify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlayListTrackUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove duplicate rows (keeping the lowest position) before enforcing uniqueness.
            migrationBuilder.Sql("""
                DELETE FROM "PlayListHasTrack" a
                USING "PlayListHasTrack" b
                WHERE a."PlayListId" = b."PlayListId"
                  AND a."TrackId" = b."TrackId"
                  AND (a."Position", a."Id") > (b."Position", b."Id");
                """);

            migrationBuilder.DropIndex(
                name: "IX_PlayListHasTrack_PlayListId",
                table: "PlayListHasTrack");

            migrationBuilder.CreateIndex(
                name: "IX_PlayListHasTrack_PlayListId_TrackId",
                table: "PlayListHasTrack",
                columns: new[] { "PlayListId", "TrackId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlayListHasTrack_PlayListId_TrackId",
                table: "PlayListHasTrack");

            migrationBuilder.CreateIndex(
                name: "IX_PlayListHasTrack_PlayListId",
                table: "PlayListHasTrack",
                column: "PlayListId");
        }
    }
}
