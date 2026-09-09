using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Musify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArtistsAndTrackOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OwnerUserId",
                table: "Tracks",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Artists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Artists_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TrackArtist",
                columns: table => new
                {
                    TrackId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtistId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackArtist", x => new { x.TrackId, x.ArtistId });
                    table.ForeignKey(
                        name: "FK_TrackArtist_Artists_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrackArtist_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_OwnerUserId",
                table: "Tracks",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Artists_ExternalId",
                table: "Artists",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Artists_NormalizedName",
                table: "Artists",
                column: "NormalizedName",
                unique: true,
                filter: "\"ExternalId\" IS NULL AND \"UserId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Artists_UserId",
                table: "Artists",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackArtist_ArtistId",
                table: "TrackArtist",
                column: "ArtistId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tracks_User_OwnerUserId",
                table: "Tracks",
                column: "OwnerUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE "Tracks" t SET "OwnerUserId" = ut."UserId"
                FROM (SELECT "TrackId", MIN("UserId") AS "UserId" FROM "UserHasTrack" GROUP BY "TrackId") ut
                WHERE t."Source" = 0 AND ut."TrackId" = t."Id";
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM "Tracks" WHERE "Source" = 0 AND "OwnerUserId" IS NULL;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "Artists" ("Id", "Name", "NormalizedName", "UserId", "ExternalId", "CreatedAt", "UpdatedAt")
                SELECT gen_random_uuid(), u."Name", u."NormalizedName", u."Id", NULL, now(), now()
                FROM "User" u
                WHERE EXISTS (SELECT 1 FROM "Tracks" t WHERE t."OwnerUserId" = u."Id");
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "Artists" ("Id", "Name", "NormalizedName", "UserId", "ExternalId", "CreatedAt", "UpdatedAt")
                SELECT gen_random_uuid(), s."Artist", upper(s."Artist"), NULL, NULL, now(), now()
                FROM (
                    SELECT DISTINCT ON (upper("Artist")) "Artist"
                    FROM "Tracks"
                    WHERE "Source" = 1 AND "Artist" IS NOT NULL
                    ORDER BY upper("Artist"), "Artist"
                ) s;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "TrackArtist" ("TrackId", "ArtistId", "Position")
                SELECT t."Id", a."Id", 0 FROM "Tracks" t
                JOIN "Artists" a ON a."UserId" = t."OwnerUserId"
                WHERE t."Source" = 0;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "TrackArtist" ("TrackId", "ArtistId", "Position")
                SELECT t."Id", a."Id", 0 FROM "Tracks" t
                JOIN "Artists" a ON a."UserId" IS NULL AND a."ExternalId" IS NULL AND a."NormalizedName" = upper(t."Artist")
                WHERE t."Source" = 1 AND t."Artist" IS NOT NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tracks_OwnerUserId_Source",
                table: "Tracks",
                sql: "(\"OwnerUserId\" IS NOT NULL) = (\"Source\" = 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tracks_User_OwnerUserId",
                table: "Tracks");

            migrationBuilder.DropTable(
                name: "TrackArtist");

            migrationBuilder.DropTable(
                name: "Artists");

            migrationBuilder.DropIndex(
                name: "IX_Tracks_OwnerUserId",
                table: "Tracks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tracks_OwnerUserId_Source",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Tracks");
        }
    }
}
