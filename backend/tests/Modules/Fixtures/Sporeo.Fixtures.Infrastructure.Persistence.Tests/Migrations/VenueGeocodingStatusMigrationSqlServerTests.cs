using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Sporeo.Fixtures.Infrastructure.Persistence.Writing;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Tests;

[Collection(SqlServerCollection.Name)]
public sealed class VenueGeocodingStatusMigrationSqlServerTests
{
    private const string DatabaseName = "SporeoFixturesVenueGeocodingMigrationTests";
    private const string PreviousMigration = "20261001172501_AddLeagueActiveMonitoredSportIdIndex";

    [Fact]
    public async Task Migration_BackfillsGeocodingStatus_FromExistingCoordinates()
    {
        await using var provider = await SqlServerTestDatabase.CreateInitializedProviderAsync(DatabaseName);
        await using var scope = provider.CreateAsyncScope();
        var migrator = scope.ServiceProvider.GetRequiredService<FixturesDbContext>().GetService<IMigrator>();

        await migrator.MigrateAsync(PreviousMigration);

        var withCoordinatesId = Guid.NewGuid();
        var withoutCoordinatesId = Guid.NewGuid();

        await using (var connection = new SqlConnection(SqlServerTestDatabase.CreateConnectionString(DatabaseName)))
        {
            await connection.OpenAsync();
            await using var insert = connection.CreateCommand();
            insert.CommandText = """
                INSERT INTO venues (Id, Name, Latitude, Longitude, IsManuallyEdited, CreatedOn, IsDeleted)
                VALUES
                    (@WithCoordinates, N'With Coordinates', 52.2297, 21.0122, 1, SYSDATETIMEOFFSET(), 0),
                    (@WithoutCoordinates, N'Without Coordinates', NULL, NULL, 1, SYSDATETIMEOFFSET(), 0);
                """;
            insert.Parameters.AddWithValue("@WithCoordinates", withCoordinatesId);
            insert.Parameters.AddWithValue("@WithoutCoordinates", withoutCoordinatesId);
            await insert.ExecuteNonQueryAsync();
        }

        await migrator.MigrateAsync();

        await using (var connection = new SqlConnection(SqlServerTestDatabase.CreateConnectionString(DatabaseName)))
        {
            await connection.OpenAsync();
            await using var select = connection.CreateCommand();
            select.CommandText = "SELECT Id, GeocodingStatus FROM venues";

            var statuses = new Dictionary<Guid, string>();
            await using var reader = await select.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                statuses[reader.GetGuid(0)] = reader.GetString(1);

            statuses.Should().BeEquivalentTo(new Dictionary<Guid, string>
            {
                [withCoordinatesId] = "Resolved",
                [withoutCoordinatesId] = "Pending"
            });
        }
    }
}
