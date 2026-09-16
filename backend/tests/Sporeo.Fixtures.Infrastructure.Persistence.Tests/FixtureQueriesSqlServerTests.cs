using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.BuildingBlocks.Domain.Time;
using Sporeo.Fixtures.Application.Fixtures.Queries.Common;
using Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtureDetails;
using Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtures;
using Sporeo.Fixtures.Application.Fixtures.Queries.GetNearbyFixtures;
using Sporeo.Fixtures.Application.Venues.Queries.GetVenueDetails;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Fixtures;
using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Seasons;
using Sporeo.Fixtures.Domain.Sports;
using Sporeo.Fixtures.Domain.Venues;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;
using Sporeo.Fixtures.Infrastructure.Persistence;
using Sporeo.Fixtures.Infrastructure.Persistence.Data;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Tests;

[Collection(SqlServerCollection.Name)]
public sealed class FixtureQueriesSqlServerTests
{
    private const string DatabaseName = "SporeoFixturesQueryTests";

    [Fact]
    public async Task GetFixtures_ShouldFilterPaginateAndHideSoftDeletedRelations()
    {
        await using var provider = await SqlServerTestDatabase.CreateInitializedProviderAsync(DatabaseName);
        var seed = await SeedAsync(provider);
        var sender = provider.GetRequiredService<ISender>();

        var all = await sender.Send(new GetFixturesQuery(
            new PaginationParams(1, 10),
            new FixtureFilters()));

        all.IsSuccess.Should().BeTrue();
        all.Value.TotalCount.Should().Be(6);
        all.Value.Items.Select(item => item.Id).Should().Equal(
            seed.FixtureWarsawLeague.Id.Value,
            seed.FixtureSoftDeletedLeague.Id.Value,
            seed.FixtureNoLocation.Id.Value,
            seed.FixtureWarsawNoLeague.Id.Value,
            seed.FixtureKrakow.Id.Value,
            seed.FixtureWithoutVenue.Id.Value);
        all.Value.Items.Should().NotContain(item => item.Id == seed.FixtureDeleted.Id.Value);
        all.Value.Items.Should().NotContain(item => item.Id == seed.FixtureDeletedSport.Id.Value);
        all.Value.Items.Should().Contain(item =>
            item.Id == seed.FixtureSoftDeletedLeague.Id.Value
            && item.LeagueName == null);

        var bySport = await sender.Send(new GetFixturesQuery(
            new PaginationParams(1, 10),
            new FixtureFilters(SportId: seed.SportFootball.Id.Value)));

        bySport.IsSuccess.Should().BeTrue();
        bySport.Value.TotalCount.Should().Be(5);
        bySport.Value.Items.Select(item => item.Id).Should().Equal(
            seed.FixtureWarsawLeague.Id.Value,
            seed.FixtureSoftDeletedLeague.Id.Value,
            seed.FixtureNoLocation.Id.Value,
            seed.FixtureWarsawNoLeague.Id.Value,
            seed.FixtureWithoutVenue.Id.Value);

        var byLeague = await sender.Send(new GetFixturesQuery(
            new PaginationParams(1, 10),
            new FixtureFilters(LeagueId: seed.FixtureWarsawLeague.LeagueId!.Value)));

        byLeague.IsSuccess.Should().BeTrue();
        byLeague.Value.Items.Should().ContainSingle()
            .Which.Id.Should().Be(seed.FixtureWarsawLeague.Id.Value);

        var byDate = await sender.Send(new GetFixturesQuery(
            new PaginationParams(1, 10),
            new FixtureFilters(
                DateFrom: seed.FixtureWarsawNoLeague.StartDate,
                DateTo: seed.FixtureWarsawNoLeague.StartDate)));

        byDate.IsSuccess.Should().BeTrue();
        byDate.Value.Items.Should().ContainSingle()
            .Which.Id.Should().Be(seed.FixtureWarsawNoLeague.Id.Value);

        var page = await sender.Send(new GetFixturesQuery(
            new PaginationParams(2, 2),
            new FixtureFilters()));

        page.IsSuccess.Should().BeTrue();
        page.Value.Items.Should().HaveCount(2);
        page.Value.TotalCount.Should().Be(6);
        page.Value.HasNextPage.Should().BeTrue();
        page.Value.Items.Select(item => item.Id).Should().Equal(
            seed.FixtureNoLocation.Id.Value,
            seed.FixtureWarsawNoLeague.Id.Value);

        var lastPage = await sender.Send(new GetFixturesQuery(
            new PaginationParams(3, 2),
            new FixtureFilters()));

        lastPage.IsSuccess.Should().BeTrue();
        lastPage.Value.Items.Should().HaveCount(2);
        lastPage.Value.HasNextPage.Should().BeFalse();

        var deletedSportFixtures = await sender.Send(new GetFixturesQuery(
            new PaginationParams(1, 10),
            new FixtureFilters(SportId: seed.SportDeleted.Id.Value)));

        deletedSportFixtures.IsSuccess.Should().BeTrue();
        deletedSportFixtures.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetFixtureDetails_ShouldMapNullableRelationsAndNotFound()
    {
        await using var provider = await SqlServerTestDatabase.CreateInitializedProviderAsync($"{DatabaseName}_Details");
        var seed = await SeedAsync(provider);
        var sender = provider.GetRequiredService<ISender>();

        var full = await sender.Send(new GetFixtureDetailsQuery(seed.FixtureWarsawLeague.Id.Value));
        full.IsSuccess.Should().BeTrue();
        full.Value.Venue.Should().NotBeNull();
        full.Value.Venue!.City.Should().Be("Warsaw");
        full.Value.Sport.Name.Should().Be("Football");
        full.Value.League.Should().NotBeNull();
        full.Value.Season.Should().NotBeNull();

        var withoutLeague = await sender.Send(new GetFixtureDetailsQuery(seed.FixtureWarsawNoLeague.Id.Value));
        withoutLeague.IsSuccess.Should().BeTrue();
        withoutLeague.Value.League.Should().BeNull();
        withoutLeague.Value.Season.Should().BeNull();
        withoutLeague.Value.Venue.Should().NotBeNull();

        var withoutVenue = await sender.Send(new GetFixtureDetailsQuery(seed.FixtureWithoutVenue.Id.Value));
        withoutVenue.IsSuccess.Should().BeTrue();
        withoutVenue.Value.Venue.Should().BeNull();

        var softDeletedLeague = await sender.Send(new GetFixtureDetailsQuery(seed.FixtureSoftDeletedLeague.Id.Value));
        softDeletedLeague.IsSuccess.Should().BeTrue();
        softDeletedLeague.Value.League.Should().BeNull();

        var deleted = await sender.Send(new GetFixtureDetailsQuery(seed.FixtureDeleted.Id.Value));
        deleted.IsFailure.Should().BeTrue();
        deleted.Error.Code.Should().Be(Errors.Fixture.NotFound(seed.FixtureDeleted.Id.Value).Code);

        var missing = await sender.Send(new GetFixtureDetailsQuery(Guid.NewGuid()));
        missing.IsFailure.Should().BeTrue();
        missing.Error.Code.Should().Be("Fixture.NotFound");

        var deletedSportFixture = await sender.Send(new GetFixtureDetailsQuery(seed.FixtureDeletedSport.Id.Value));
        deletedSportFixture.IsFailure.Should().BeTrue();
        deletedSportFixture.Error.Code.Should().Be("Fixture.NotFound");
    }

    [Fact]
    public async Task GetVenueDetails_ShouldReturnCoordinatesAndNotFound()
    {
        await using var provider = await SqlServerTestDatabase.CreateInitializedProviderAsync($"{DatabaseName}_Venues");
        var seed = await SeedAsync(provider);
        var sender = provider.GetRequiredService<ISender>();

        var warsaw = await sender.Send(new GetVenueDetailsQuery(seed.VenueWarsaw.Id.Value));
        warsaw.IsSuccess.Should().BeTrue();
        warsaw.Value.Latitude.Should().BeApproximately(52.2297, 0.0001);
        warsaw.Value.Longitude.Should().BeApproximately(21.0122, 0.0001);
        warsaw.Value.City.Should().Be("Warsaw");

        var withoutCoordinates = await sender.Send(new GetVenueDetailsQuery(seed.VenueWithoutCoordinates.Id.Value));
        withoutCoordinates.IsSuccess.Should().BeTrue();
        withoutCoordinates.Value.Latitude.Should().BeNull();
        withoutCoordinates.Value.Longitude.Should().BeNull();

        var deleted = await sender.Send(new GetVenueDetailsQuery(seed.VenueDeleted.Id.Value));
        deleted.IsFailure.Should().BeTrue();
        deleted.Error.Code.Should().Be("Venue.NotFound");

        var missing = await sender.Send(new GetVenueDetailsQuery(Guid.NewGuid()));
        missing.IsFailure.Should().BeTrue();
        missing.Error.Code.Should().Be("Venue.NotFound");
    }

    [Fact]
    public async Task GetNearbyFixtures_ShouldFilterByRadiusAndExcludeMissingLocation()
    {
        await using var provider = await SqlServerTestDatabase.CreateInitializedProviderAsync($"{DatabaseName}_Nearby");
        var seed = await SeedAsync(provider);
        var sender = provider.GetRequiredService<ISender>();

        var nearbyWarsaw = await sender.Send(new GetNearbyFixturesQuery(
            Latitude: 52.2297,
            Longitude: 21.0122,
            RadiusInMeters: 1_000,
            Pagination: new PaginationParams(1, 10),
            Filters: new FixtureFilters()));

        nearbyWarsaw.IsSuccess.Should().BeTrue();
        nearbyWarsaw.Value.TotalCount.Should().Be(3);
        nearbyWarsaw.Value.Items.Select(item => item.Id).Should().BeEquivalentTo(
        [
            seed.FixtureWarsawLeague.Id.Value,
            seed.FixtureSoftDeletedLeague.Id.Value,
            seed.FixtureWarsawNoLeague.Id.Value
        ]);
        nearbyWarsaw.Value.Items.Should().OnlyContain(item => item.DistanceInMeters >= 0);
        nearbyWarsaw.Value.Items.Should().BeInAscendingOrder(item => item.DistanceInMeters);

        var wider = await sender.Send(new GetNearbyFixturesQuery(
            Latitude: 52.2297,
            Longitude: 21.0122,
            RadiusInMeters: 500_000,
            Pagination: new PaginationParams(1, 10),
            Filters: new FixtureFilters()));

        wider.IsSuccess.Should().BeTrue();
        wider.Value.Items.Select(item => item.Id).Should().BeEquivalentTo(
        [
            seed.FixtureWarsawLeague.Id.Value,
            seed.FixtureSoftDeletedLeague.Id.Value,
            seed.FixtureWarsawNoLeague.Id.Value,
            seed.FixtureKrakow.Id.Value
        ]);
        wider.Value.Items.Should().NotContain(item => item.Id == seed.FixtureWithoutVenue.Id.Value);
        wider.Value.Items.Should().NotContain(item => item.Id == seed.FixtureNoLocation.Id.Value);
        wider.Value.Items.Should().NotContain(item => item.Id == seed.FixtureDeleted.Id.Value);

        var basketballOnly = await sender.Send(new GetNearbyFixturesQuery(
            Latitude: 52.2297,
            Longitude: 21.0122,
            RadiusInMeters: 500_000,
            Pagination: new PaginationParams(1, 10),
            Filters: new FixtureFilters(SportId: seed.SportBasketball.Id.Value)));

        basketballOnly.IsSuccess.Should().BeTrue();
        basketballOnly.Value.Items.Should().ContainSingle()
            .Which.Id.Should().Be(seed.FixtureKrakow.Id.Value);
    }

    private static async Task<SeedData> SeedAsync(ServiceProvider provider)
    {
        var originalTimeProvider = SystemTimeProvider.Provider;
        SystemTimeProvider.Provider = new FixedTimeProvider(new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero));

        try
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<FixturesDbContext>();

            var sportFootball = Sport.Create("Football").Value;
            var sportBasketball = Sport.Create("Basketball").Value;
            var sportDeleted = Sport.Create("Deleted Sport").Value;

            var leagueActive = League.CreateManually(sportFootball.Id, "Ekstraklasa", "PL").Value;
            var leagueSoftDeleted = League.CreateManually(sportFootball.Id, "Deleted League", "PL").Value;

            var season = Season.CreateManually(
                leagueActive.Id,
                "2025/26",
                new DateTimeOffset(2025, 7, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero)).Value;

            var venueWarsaw = Venue.CreateManually(
                "National Stadium",
                Address.Create("ul. Pilsudskiego 1", "Warsaw", "Poland").Value,
                Coordinates.Create(52.2297, 21.0122).Value).Value;
            var venueKrakow = Venue.CreateManually(
                "Tauron Arena",
                Address.Create("ul. Reymonta 1", "Krakow", "Poland").Value,
                Coordinates.Create(50.0647, 19.9450).Value).Value;
            var venueWithoutCoordinates = Venue.CreateManually("Hall Without Location").Value;
            var venueDeleted = Venue.CreateManually(
                "Deleted Venue",
                Address.Create("ul. Pilsudskiego 1", "Warsaw", "Poland").Value,
                Coordinates.Create(52.2297, 21.0122).Value).Value;

            var start1 = new DateTimeOffset(2026, 10, 1, 18, 0, 0, TimeSpan.Zero);
            var start2 = new DateTimeOffset(2026, 10, 2, 18, 0, 0, TimeSpan.Zero);
            var start3 = new DateTimeOffset(2026, 10, 3, 18, 0, 0, TimeSpan.Zero);
            var start4 = new DateTimeOffset(2026, 10, 4, 18, 0, 0, TimeSpan.Zero);

            var fixtureWarsawLeague = Fixture.CreateManually(
                sportFootball.Id, leagueActive.Id, season.Id, "Warsaw League Match", start1).Value;
            fixtureWarsawLeague.AssignVenue(venueWarsaw.Id);

            var fixtureWarsawNoLeague = Fixture.CreateManually(
                sportFootball.Id, null, null, "Warsaw Friendly", start2).Value;
            fixtureWarsawNoLeague.AssignVenue(venueWarsaw.Id);

            var fixtureKrakow = Fixture.CreateManually(
                sportBasketball.Id, null, null, "Krakow Match", start3).Value;
            fixtureKrakow.AssignVenue(venueKrakow.Id);

            var fixtureWithoutVenue = Fixture.CreateManually(
                sportFootball.Id, null, null, "No Venue Match", start4).Value;

            var fixtureSoftDeletedLeague = Fixture.CreateManually(
                sportFootball.Id, leagueSoftDeleted.Id, null, "Soft Deleted League Match",
                start1.AddHours(1)).Value;
            fixtureSoftDeletedLeague.AssignVenue(venueWarsaw.Id);

            var fixtureDeleted = Fixture.CreateManually(
                sportFootball.Id, null, null, "Deleted Match", start1.AddHours(2)).Value;
            fixtureDeleted.AssignVenue(venueWarsaw.Id);
            fixtureDeleted.Delete();

            var fixtureDeletedSport = Fixture.CreateManually(
                sportDeleted.Id, null, null, "Deleted Sport Match", start1.AddHours(3)).Value;
            fixtureDeletedSport.AssignVenue(venueWarsaw.Id);

            var fixtureNoLocation = Fixture.CreateManually(
                sportFootball.Id, null, null, "No Location Match", start1.AddHours(4)).Value;
            fixtureNoLocation.AssignVenue(venueWithoutCoordinates.Id);

            db.AddRange(
                sportFootball,
                sportBasketball,
                sportDeleted,
                leagueActive,
                leagueSoftDeleted,
                season,
                venueWarsaw,
                venueKrakow,
                venueWithoutCoordinates,
                venueDeleted,
                fixtureWarsawLeague,
                fixtureWarsawNoLeague,
                fixtureKrakow,
                fixtureWithoutVenue,
                fixtureSoftDeletedLeague,
                fixtureDeleted,
                fixtureDeletedSport,
                fixtureNoLocation);

            await db.SaveChangesAsync();

            leagueSoftDeleted.Delete();
            venueDeleted.Delete();
            sportDeleted.Delete();
            await db.SaveChangesAsync();

            return new SeedData(
                sportFootball,
                sportBasketball,
                sportDeleted,
                venueWarsaw,
                venueWithoutCoordinates,
                venueDeleted,
                fixtureWarsawLeague,
                fixtureWarsawNoLeague,
                fixtureKrakow,
                fixtureWithoutVenue,
                fixtureSoftDeletedLeague,
                fixtureDeleted,
                fixtureDeletedSport,
                fixtureNoLocation);
        }
        finally
        {
            SystemTimeProvider.Provider = originalTimeProvider;
        }
    }

    private sealed record SeedData(
        Sport SportFootball,
        Sport SportBasketball,
        Sport SportDeleted,
        Venue VenueWarsaw,
        Venue VenueWithoutCoordinates,
        Venue VenueDeleted,
        Fixture FixtureWarsawLeague,
        Fixture FixtureWarsawNoLeague,
        Fixture FixtureKrakow,
        Fixture FixtureWithoutVenue,
        Fixture FixtureSoftDeletedLeague,
        Fixture FixtureDeleted,
        Fixture FixtureDeletedSport,
        Fixture FixtureNoLocation);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
