using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sporeo.Fixtures.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddOutboxMessages : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                OccurredOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                ProcessedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                RetryCount = table.Column<int>(type: "int", nullable: false),
                NextAttempt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OutboxMessages", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_Pending",
            table: "OutboxMessages",
            columns: new[] { "Status", "NextAttempt", "OccurredOn" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "OutboxMessages");
    }
}
