using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Musify.Domain.ValueObjects;

#nullable disable

namespace Musify.Infrastructure.Persistence.Migrations
{

    public partial class AddTrackTags : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:genre", "ambient,blues,classical,country,drum_and_bass,dubstep,electronic,folk,funk,hip_hop,house,indie,jazz,k_pop,latin,lofi,metal,pop,punk,reggae,reggaeton,rn_b,rock,soul,techno,trance");

            migrationBuilder.CreateTable(
                name: "TrackTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tag = table.Column<Genre>(type: "genre", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackTags_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackTags_TrackId_Tag",
                table: "TrackTags",
                columns: new[] { "TrackId", "Tag" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackTags");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:genre", "ambient,blues,classical,country,drum_and_bass,dubstep,electronic,folk,funk,hip_hop,house,indie,jazz,k_pop,latin,lofi,metal,pop,punk,reggae,reggaeton,rn_b,rock,soul,techno,trance");
        }
    }
}
