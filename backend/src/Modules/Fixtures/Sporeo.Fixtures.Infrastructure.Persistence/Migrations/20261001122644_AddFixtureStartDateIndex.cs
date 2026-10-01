using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sporeo.Fixtures.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFixtureStartDateIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_fixtures_StartDate",
                table: "fixtures",
                column: "StartDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_fixtures_StartDate",
                table: "fixtures");
        }
    }
}
