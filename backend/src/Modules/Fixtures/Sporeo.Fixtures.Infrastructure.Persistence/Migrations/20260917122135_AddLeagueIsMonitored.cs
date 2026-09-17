using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sporeo.Fixtures.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeagueIsMonitored : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMonitored",
                table: "leagues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_ActiveMonitored",
                table: "leagues",
                column: "IsMonitored",
                filter: "[IsMonitored] = 1 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leagues_ActiveMonitored",
                table: "leagues");

            migrationBuilder.DropColumn(
                name: "IsMonitored",
                table: "leagues");
        }
    }
}
