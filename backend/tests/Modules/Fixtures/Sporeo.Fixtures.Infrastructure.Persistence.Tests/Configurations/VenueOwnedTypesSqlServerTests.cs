using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Sporeo.BuildingBlocks.Domain.Time;
using Sporeo.Fixtures.Domain.Fixtures;
using Sporeo.Fixtures.Domain.Sports;
using Sporeo.Fixtures.Domain.Venues;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;
using Sporeo.Fixtures.Infrastructure.Persistence;
using Sporeo.Fixtures.Infrastructure.Persistence.Context;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Tests;

[Collection(SqlServerCollection.Name)]
public sealed class VenueOwnedTypesSqlServerTests
{
    [Fact]
    public async Task SaveChanges_ShouldPersistAddressCoordinatesAndLocation()
    {
        const string databaseName = "SporeoFixturesOwnedTypesTests";
        await using var provider = await SqlServerTestDatabase.CreateInitializedProviderAsync(databaseName);

        Guid venueId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FixturesDbContext>();
            var coords = Coordinates.Create(52.2297, 21.0122).Value;
            var address = Address.Create("ul. Test 1", "Warsaw", "Poland").Value;
            var venue = Venue.CreateManually("Diag Venue", address, coords).Value;
            venueId = venue.Id.Value;
            db.Venues.Add(venue);
            await db.SaveChangesAsync();
        }

        await AssertVenuePersistedAsync(databaseName, venueId, expectLocation: true);
    }

    [Fact]
    public async Task SaveChanges_WithAddRangeGraph_ShouldPersistOwnedVenueTypes()
    {
        const string databaseName = "SporeoFixturesOwnedTypesGraphTests";
        var original = SystemTimeProvider.Provider;
        SystemTimeProvider.Provider = new FixedTimeProvider(new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero));

        try
        {
            await using var provider = await SqlServerTestDatabase.CreateInitializedProviderAsync(databaseName);
            Guid venueId;

            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<FixturesDbContext>();
                var sport = Sport.Create("Football").Value;
                var coords = Coordinates.Create(52.2297, 21.0122).Value;
                var address = Address.Create("ul. Test 1", "Warsaw", "Poland").Value;
                var venue = Venue.CreateManually("Diag Venue", address, coords).Value;
                var fixture = Fixture.CreateManually(
                    sport.Id,
                    null,
                    null,
                    "Match",
                    new DateTimeOffset(2026, 10, 1, 18, 0, 0, TimeSpan.Zero)).Value;
                fixture.AssignVenue(venue.Id);

                venueId = venue.Id.Value;
                db.AddRange(sport, venue, fixture);
                await db.SaveChangesAsync();
            }

            await AssertVenuePersistedAsync(databaseName, venueId, expectLocation: true);
        }
        finally
        {
            SystemTimeProvider.Provider = original;
        }
    }

    private static async Task AssertVenuePersistedAsync(string databaseName, Guid venueId, bool expectLocation)
    {
        await using var connection = new SqlConnection(SqlServerTestDatabase.CreateConnectionString(databaseName));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Street, City, Country, Latitude, Longitude,
                   CASE WHEN Location IS NULL THEN 1 ELSE 0 END AS LocationIsNull
            FROM venues
            WHERE Id = @Id
            """;
        command.Parameters.AddWithValue("@Id", venueId);

        await using var reader = await command.ExecuteReaderAsync();
        reader.Read().Should().BeTrue();
        reader.GetString(0).Should().Be("ul. Test 1");
        reader.GetString(1).Should().Be("Warsaw");
        reader.GetString(2).Should().Be("Poland");
        reader.GetDecimal(3).Should().Be(52.2297m);
        reader.GetDecimal(4).Should().Be(21.0122m);
        reader.GetInt32(5).Should().Be(expectLocation ? 0 : 1);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
