using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Musify.Infrastructure.Persistence.Migrations
{

    public partial class SplitTrackIntoLocalAndExternal : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrackArtist_Tracks_TrackId",
                table: "TrackArtist");

            migrationBuilder.DropForeignKey(
                name: "FK_Tracks_User_OwnerUserId",
                table: "Tracks");

            migrationBuilder.DropIndex(
                name: "IX_Tracks_OwnerUserId",
                table: "Tracks");

            migrationBuilder.DropIndex(
                name: "IX_Tracks_Source_ExternalId",
                table: "Tracks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tracks_OwnerUserId_Source",
                table: "Tracks");

            migrationBuilder.CreateTable(
                name: "ExternalTracks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalTracks_Tracks_Id",
                        column: x => x.Id,
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocalTracks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalTracks_Tracks_Id",
                        column: x => x.Id,
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LocalTracks_User_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalTracks_Source_ExternalId",
                table: "ExternalTracks",
                columns: new[] { "Source", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalTracks_OwnerUserId",
                table: "LocalTracks",
                column: "OwnerUserId");

            migrationBuilder.Sql(
                """
                INSERT INTO "LocalTracks" ("Id", "OwnerUserId")
                SELECT "Id", "OwnerUserId" FROM "Tracks" WHERE "Source" = 0;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "ExternalTracks" ("Id", "Source", "ExternalId")
                SELECT "Id", "Source", "ExternalId" FROM "Tracks" WHERE "Source" <> 0;
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM "TrackArtist" tra
                USING "Tracks" t
                WHERE tra."TrackId" = t."Id" AND t."Source" = 0;
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM "Artists" WHERE "UserId" IS NOT NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_TrackArtist_ExternalTracks_TrackId",
                table: "TrackArtist",
                column: "TrackId",
                principalTable: "ExternalTracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Tracks");

            migrationBuilder.DropIndex(
                name: "IX_Artists_NormalizedName",
                table: "Artists");

            migrationBuilder.DropIndex(
                name: "IX_Artists_UserId",
                table: "Artists");

            migrationBuilder.DropForeignKey(
                name: "FK_Artists_User_UserId",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Artists");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalId",
                table: "Artists",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrackArtist_ExternalTracks_TrackId",
                table: "TrackArtist");

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Tracks",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OwnerUserId",
                table: "Tracks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Tracks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE "Tracks" t SET "OwnerUserId" = lt."OwnerUserId", "Source" = 0
                FROM "LocalTracks" lt WHERE lt."Id" = t."Id";
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Tracks" t SET "ExternalId" = et."ExternalId", "Source" = et."Source"
                FROM "ExternalTracks" et WHERE et."Id" = t."Id";
                """);

            migrationBuilder.DropTable(
                name: "ExternalTracks");

            migrationBuilder.DropTable(
                name: "LocalTracks");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalId",
                table: "Artists",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "Artists",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_OwnerUserId",
                table: "Tracks",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_Source_ExternalId",
                table: "Tracks",
                columns: new[] { "Source", "ExternalId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tracks_OwnerUserId_Source",
                table: "Tracks",
                sql: "(\"OwnerUserId\" IS NOT NULL) = (\"Source\" = 0)");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Artists_User_UserId",
                table: "Artists",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TrackArtist_Tracks_TrackId",
                table: "TrackArtist",
                column: "TrackId",
                principalTable: "Tracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tracks_User_OwnerUserId",
                table: "Tracks",
                column: "OwnerUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
