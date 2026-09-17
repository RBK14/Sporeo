using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sporeo.Fixtures.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGeocodingCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OutboxMessages",
                table: "OutboxMessages");

            migrationBuilder.RenameTable(
                name: "OutboxMessages",
                newName: "outbox-messages");

            migrationBuilder.AddPrimaryKey(
                name: "PK_outbox-messages",
                table: "outbox-messages",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "geocoding-cache-entries",
                columns: table => new
                {
                    NormalizedAddress = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    IsFound = table.Column<bool>(type: "bit", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    Street = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CachedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_geocoding-cache-entries", x => x.NormalizedAddress);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeocodingCacheEntries_ExpiresAtUtc",
                table: "geocoding-cache-entries",
                column: "ExpiresAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "geocoding-cache-entries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_outbox-messages",
                table: "outbox-messages");

            migrationBuilder.RenameTable(
                name: "outbox-messages",
                newName: "OutboxMessages");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OutboxMessages",
                table: "OutboxMessages",
                column: "Id");
        }
    }
}
