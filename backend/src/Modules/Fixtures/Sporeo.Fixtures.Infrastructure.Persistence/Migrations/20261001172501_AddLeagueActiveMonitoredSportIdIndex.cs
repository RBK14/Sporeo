using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sporeo.Fixtures.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeagueActiveMonitoredSportIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leagues_ActiveMonitored",
                table: "leagues");

            migrationBuilder.CreateIndex(
                name: "IX_leagues_ActiveMonitored_SportId",
                table: "leagues",
                column: "SportId",
                filter: "[IsMonitored] = 1 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_leagues_ActiveMonitored_SportId",
                table: "leagues");

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_ActiveMonitored",
                table: "leagues",
                column: "IsMonitored",
                filter: "[IsMonitored] = 1 AND [IsDeleted] = 0");
        }
    }
}
