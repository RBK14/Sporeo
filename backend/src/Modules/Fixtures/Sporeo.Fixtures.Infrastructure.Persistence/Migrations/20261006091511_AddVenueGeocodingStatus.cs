using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sporeo.Fixtures.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueGeocodingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GeocodingErrorCode",
                table: "venues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeocodingStatus",
                table: "venues",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastGeocodingAttemptOn",
                table: "venues",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE venues
                SET GeocodingStatus = CASE WHEN Latitude IS NULL THEN 'Pending' ELSE 'Resolved' END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeocodingErrorCode",
                table: "venues");

            migrationBuilder.DropColumn(
                name: "GeocodingStatus",
                table: "venues");

            migrationBuilder.DropColumn(
                name: "LastGeocodingAttemptOn",
                table: "venues");
        }
    }
}
