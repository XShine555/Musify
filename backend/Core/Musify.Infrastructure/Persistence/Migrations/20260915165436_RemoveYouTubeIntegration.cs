using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Musify.Infrastructure.Persistence.Migrations
{

    public partial class RemoveYouTubeIntegration : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql("""DELETE FROM "Tracks" WHERE "Id" IN (SELECT "Id" FROM "ExternalTracks");""");
            migrationBuilder.Sql("""DELETE FROM "Albums" WHERE "Id" IN (SELECT "Id" FROM "ExternalAlbums");""");

            migrationBuilder.Sql("""DELETE FROM "MixItems" WHERE "TrackId" IS NULL;""");

            migrationBuilder.AddColumn<long>(
                name: "OwnerUserId",
                table: "Tracks",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql("""
                UPDATE "Tracks" t SET "OwnerUserId" = lt."OwnerUserId"
                FROM "LocalTracks" lt WHERE lt."Id" = t."Id";
                """);

            migrationBuilder.AddColumn<long>(
                name: "OwnerUserId",
                table: "Albums",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql("""
                UPDATE "Albums" a SET "OwnerUserId" = ua."OwnerUserId"
                FROM "UserAlbums" ua WHERE ua."Id" = a."Id";
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "TrackId",
                table: "MixItems",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Artist",
                table: "MixItems");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "MixItems");

            migrationBuilder.DropColumn(
                name: "IsExplicit",
                table: "MixItems");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "MixItems");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "MixItems");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "MixItems");

            migrationBuilder.DropColumn(
                name: "VideoId",
                table: "MixItems");

            migrationBuilder.DropTable(
                name: "TrackArtist");

            migrationBuilder.DropTable(
                name: "ExternalTracks");

            migrationBuilder.DropTable(
                name: "LocalTracks");

            migrationBuilder.DropTable(
                name: "ExternalAlbums");

            migrationBuilder.DropTable(
                name: "UserAlbums");

            migrationBuilder.DropTable(
                name: "Artists");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_OwnerUserId",
                table: "Tracks",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Albums_OwnerUserId",
                table: "Albums",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Albums_User_OwnerUserId",
                table: "Albums",
                column: "OwnerUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tracks_User_OwnerUserId",
                table: "Tracks",
                column: "OwnerUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Albums_User_OwnerUserId",
                table: "Albums");

            migrationBuilder.DropForeignKey(
                name: "FK_Tracks_User_OwnerUserId",
                table: "Tracks");

            migrationBuilder.DropIndex(
                name: "IX_Tracks_OwnerUserId",
                table: "Tracks");

            migrationBuilder.DropIndex(
                name: "IX_Albums_OwnerUserId",
                table: "Albums");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Tracks");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Albums");

            migrationBuilder.AlterColumn<Guid>(
                name: "TrackId",
                table: "MixItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "Artist",
                table: "MixItems",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DurationSeconds",
                table: "MixItems",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "IsExplicit",
                table: "MixItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "MixItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "MixItems",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "MixItems",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoId",
                table: "MixItems",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Artists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalAlbums",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAlbums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalAlbums_Albums_Id",
                        column: x => x.Id,
                        principalTable: "Albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExternalTracks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "UserAlbums",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAlbums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAlbums_Albums_Id",
                        column: x => x.Id,
                        principalTable: "Albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAlbums_User_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                        name: "FK_TrackArtist_ExternalTracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "ExternalTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Artists_ExternalId",
                table: "Artists",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAlbums_Source_ExternalId",
                table: "ExternalAlbums",
                columns: new[] { "Source", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalTracks_Source_ExternalId",
                table: "ExternalTracks",
                columns: new[] { "Source", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalTracks_OwnerUserId",
                table: "LocalTracks",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackArtist_ArtistId",
                table: "TrackArtist",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAlbums_OwnerUserId",
                table: "UserAlbums",
                column: "OwnerUserId");
        }
    }
}
