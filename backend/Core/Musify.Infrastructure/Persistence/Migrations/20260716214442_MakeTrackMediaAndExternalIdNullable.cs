using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Musify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeTrackMediaAndExternalIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tracks_Source_ExternalId",
                table: "Tracks");

            migrationBuilder.AlterColumn<string>(
                name: "OriginalPictureName",
                table: "Tracks",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "OriginalAudioName",
                table: "Tracks",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalId",
                table: "Tracks",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<string>(
                name: "Artist",
                table: "Tracks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.Sql("UPDATE \"Tracks\" SET \"ExternalId\" = NULL WHERE \"ExternalId\" = '';");
            migrationBuilder.Sql("UPDATE \"Tracks\" SET \"Artist\" = NULL WHERE \"Artist\" = '';");
            migrationBuilder.Sql("UPDATE \"Tracks\" SET \"OriginalPictureName\" = NULL WHERE \"OriginalPictureName\" = '';");
            migrationBuilder.Sql("UPDATE \"Tracks\" SET \"OriginalAudioName\" = NULL WHERE \"OriginalAudioName\" = '';");

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_Source_ExternalId",
                table: "Tracks",
                columns: new[] { "Source", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tracks_Source_ExternalId",
                table: "Tracks");

            migrationBuilder.AlterColumn<string>(
                name: "OriginalPictureName",
                table: "Tracks",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OriginalAudioName",
                table: "Tracks",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalId",
                table: "Tracks",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Artist",
                table: "Tracks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_Source_ExternalId",
                table: "Tracks",
                columns: new[] { "Source", "ExternalId" },
                unique: true,
                filter: "\"ExternalId\" <> ''");
        }
    }
}
