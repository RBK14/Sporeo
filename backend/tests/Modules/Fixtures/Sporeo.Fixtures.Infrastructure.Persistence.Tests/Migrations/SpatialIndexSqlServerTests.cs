using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using Sporeo.Fixtures.Application;
using Sporeo.Fixtures.Domain.Venues;
using Sporeo.Fixtures.Infrastructure.Persistence;
using Sporeo.Fixtures.Infrastructure.Persistence.Context;
using DomainCoordinates = Sporeo.Fixtures.Domain.Venues.ValueObjects.Coordinates;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Tests;

[Collection(SqlServerCollection.Name)]
public sealed class SpatialIndexSqlServerTests
{
    private const string DatabaseName = "SporeoFixturesSpatialTests";
    private const string MasterConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
    private const string ConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=SporeoFixturesSpatialTests;Trusted_Connection=True;TrustServerCertificate=True;";

    [Fact]
    public async Task Migration_CreatesSpatialIndex_AndSupportsDistanceQuery()
    {
        await EnsureLocalDbAvailableAsync();
        await RecreateDatabaseAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:fixtures-db"] = ConnectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddPersistence(configuration);

        await using var provider = services.BuildServiceProvider();
        await provider.InitializeFixturesDatabaseAsync();

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FixturesDbContext>();

        var coordinates = DomainCoordinates.Create(52.2297, 21.0122).Value;
        db.Venues.Add(Venue.CreateManually("National Stadium", coordinates: coordinates).Value);
        await db.SaveChangesAsync();

        var origin = new Point(21.0122, 52.2297) { SRID = 4326 };
        var nearby = await db.Venues
            .AsNoTracking()
            .Where(v => EF.Property<Point>(v, "Location").IsWithinDistance(origin, 1_000))
            .ToListAsync();

        nearby.Should().ContainSingle();

        await using var verifyConnection = new SqlConnection(ConnectionString);
        await verifyConnection.OpenAsync();
        await using var indexCommand = verifyConnection.CreateCommand();
        indexCommand.CommandText = """
            SELECT i.name
            FROM sys.spatial_indexes AS i
            INNER JOIN sys.tables AS t ON i.object_id = t.object_id
            WHERE t.name = N'venues' AND i.name = N'IX_venues_Location';
            """;

        var indexName = (string?)await indexCommand.ExecuteScalarAsync();
        indexName.Should().Be("IX_venues_Location");

        await using var planOn = new SqlCommand("SET SHOWPLAN_XML ON;", verifyConnection);
        await planOn.ExecuteNonQueryAsync();

        string planXml;
        await using (var planQuery = new SqlCommand("""
            DECLARE @origin geography = geography::Point(52.2297, 21.0122, 4326);
            SELECT COUNT_BIG(*)
            FROM venues WITH (INDEX([IX_venues_Location]))
            WHERE Location.STDistance(@origin) <= 1000;
            """, verifyConnection))
        await using (var reader = await planQuery.ExecuteReaderAsync())
        {
            reader.Read().Should().BeTrue();
            planXml = reader.GetString(0);
        }

        await using var planOff = new SqlCommand("SET SHOWPLAN_XML OFF;", verifyConnection);
        await planOff.ExecuteNonQueryAsync();

        planXml.Should().ContainEquivalentOf("IX_venues_Location");
    }

    private static async Task EnsureLocalDbAvailableAsync()
    {
        try
        {
            await using var connection = new SqlConnection(MasterConnectionString);
            await connection.OpenAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "SQL Server LocalDB is required for spatial index verification.", ex);
        }
    }

    private static async Task RecreateDatabaseAsync()
    {
        await using var connection = new SqlConnection(MasterConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID(N'{DatabaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{DatabaseName}];
            END
            """;
        await command.ExecuteNonQueryAsync();
    }
}
