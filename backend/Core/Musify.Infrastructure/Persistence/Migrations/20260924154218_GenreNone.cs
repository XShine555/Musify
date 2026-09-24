using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Musify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GenreNone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:genre", "ambient,blues,classical,country,drum_and_bass,dubstep,electronic,folk,funk,hip_hop,house,indie,jazz,k_pop,latin,lofi,metal,none,pop,punk,reggae,reggaeton,rn_b,rock,soul,techno,trance")
                .OldAnnotation("Npgsql:Enum:genre", "ambient,blues,classical,country,drum_and_bass,dubstep,electronic,folk,funk,hip_hop,house,indie,jazz,k_pop,latin,lofi,metal,pop,punk,reggae,reggaeton,rn_b,rock,soul,techno,trance");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:genre", "ambient,blues,classical,country,drum_and_bass,dubstep,electronic,folk,funk,hip_hop,house,indie,jazz,k_pop,latin,lofi,metal,pop,punk,reggae,reggaeton,rn_b,rock,soul,techno,trance")
                .OldAnnotation("Npgsql:Enum:genre", "ambient,blues,classical,country,drum_and_bass,dubstep,electronic,folk,funk,hip_hop,house,indie,jazz,k_pop,latin,lofi,metal,none,pop,punk,reggae,reggaeton,rn_b,rock,soul,techno,trance");
        }
    }
}
