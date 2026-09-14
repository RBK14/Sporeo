using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sporeo.Fixtures.Domain.Fixtures;
using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Seasons;
using Sporeo.Fixtures.Domain.Sports;
using Sporeo.Fixtures.Domain.Venues;
using Sporeo.Fixtures.Domain.Fixtures.ValueObjects;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;
using Sporeo.Fixtures.Infrastructure.Persistence.Contexts;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Tests;

[Collection(SqlServerCollection.Name)]
public sealed class RepositorySqlServerTests
{
    private const string DatabaseName = "SporeoFixturesRepositoryTests";

    [Fact]
    public async Task Repositories_ShouldAddGetByIdAndHideSoftDeletedAggregates()
    {
        await using var provider = await SqlServerTestDatabase.CreateInitializedProviderAsync(DatabaseName);

        FixtureId fixtureId;
        VenueId venueId;
        SeasonId seasonId;
        LeagueId leagueId;
        SportId sportId;

        await using (var scope = provider.CreateAsyncScope())
        {
            var fixtureRepository = scope.ServiceProvider.GetRequiredService<IFixtureRepository>();
            var venueRepository = scope.ServiceProvider.GetRequiredService<IVenueRepository>();
            var leagueRepository = scope.ServiceProvider.GetRequiredService<ILeagueRepository>();
            var seasonRepository = scope.ServiceProvider.GetRequiredService<ISeasonRepository>();
            var sportRepository = scope.ServiceProvider.GetRequiredService<ISportRepository>();
            var db = scope.ServiceProvider.GetRequiredService<FixturesDbContext>();

            var sport = Sport.Create("Football", "provider", "sport-1").Value;
            var league = League.CreateFromProvider(sport.Id, "Premier League", "England", "provider", "league-1").Value;
            var season = Season.CreateFromProvider(
                league.Id,
                "2025/26",
                new DateTimeOffset(2025, 8, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 5, 31, 0, 0, 0, TimeSpan.Zero),
                "provider",
                "season-1").Value;
            var venue = Venue.CreateFromProvider("Stadium", "provider", "venue-1").Value;
            var fixture = Fixture.CreateFromProvider(
                sport.Id,
                league.Id,
                season.Id,
                "Team A vs Team B",
                new DateTimeOffset(2025, 9, 14, 15, 0, 0, TimeSpan.Zero),
                "provider",
                "fixture-1").Value;

            sportId = sport.Id;
            leagueId = league.Id;
            seasonId = season.Id;
            venueId = venue.Id;
            fixtureId = fixture.Id;

            sportRepository.Add(sport);
            leagueRepository.Add(league);
            seasonRepository.Add(season);
            venueRepository.Add(venue);
            fixtureRepository.Add(fixture);
            await db.SaveChangesAsync();

            (await sportRepository.GetByIdAsync(sport.Id)).Should().NotBeNull();
            (await leagueRepository.GetByIdAsync(league.Id)).Should().NotBeNull();
            (await seasonRepository.GetByIdAsync(season.Id)).Should().NotBeNull();
            (await venueRepository.GetByIdAsync(venue.Id)).Should().NotBeNull();
            (await fixtureRepository.GetByIdAsync(fixture.Id)).Should().NotBeNull();

            (await sportRepository.GetByExternalProviderAsync("provider", "sport-1"))!.Id.Should().Be(sport.Id);
            (await leagueRepository.GetByExternalProviderAsync("provider", "league-1"))!.Id.Should().Be(league.Id);
            (await seasonRepository.GetByExternalProviderAsync("provider", "season-1"))!.Id.Should().Be(season.Id);
            (await venueRepository.GetByExternalProviderAsync("provider", "venue-1"))!.Id.Should().Be(venue.Id);
            (await fixtureRepository.GetByExternalProviderAsync("provider", "fixture-1"))!.Id.Should().Be(fixture.Id);

            fixture.Delete().IsSuccess.Should().BeTrue();
            venue.Delete().IsSuccess.Should().BeTrue();
            season.Delete().IsSuccess.Should().BeTrue();
            league.Delete().IsSuccess.Should().BeTrue();
            sport.Delete().IsSuccess.Should().BeTrue();
            await db.SaveChangesAsync();
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var fixtureRepository = scope.ServiceProvider.GetRequiredService<IFixtureRepository>();
            var venueRepository = scope.ServiceProvider.GetRequiredService<IVenueRepository>();
            var leagueRepository = scope.ServiceProvider.GetRequiredService<ILeagueRepository>();
            var seasonRepository = scope.ServiceProvider.GetRequiredService<ISeasonRepository>();
            var sportRepository = scope.ServiceProvider.GetRequiredService<ISportRepository>();

            (await fixtureRepository.GetByIdAsync(fixtureId)).Should().BeNull();
            (await venueRepository.GetByIdAsync(venueId)).Should().BeNull();
            (await seasonRepository.GetByIdAsync(seasonId)).Should().BeNull();
            (await leagueRepository.GetByIdAsync(leagueId)).Should().BeNull();
            (await sportRepository.GetByIdAsync(sportId)).Should().BeNull();

            (await fixtureRepository.GetByExternalProviderAsync("provider", "fixture-1")).Should().BeNull();
            (await venueRepository.GetByExternalProviderAsync("provider", "venue-1")).Should().BeNull();
            (await seasonRepository.GetByExternalProviderAsync("provider", "season-1")).Should().BeNull();
            (await leagueRepository.GetByExternalProviderAsync("provider", "league-1")).Should().BeNull();
            (await sportRepository.GetByExternalProviderAsync("provider", "sport-1")).Should().BeNull();
        }
    }

    [Fact]
    public async Task FixturesAndVenues_ShouldGetByExternalProviderIds()
    {
        await using var provider = await SqlServerTestDatabase.CreateInitializedProviderAsync(
            $"{DatabaseName}_Batch");

        await using var scope = provider.CreateAsyncScope();
        var fixtureRepository = scope.ServiceProvider.GetRequiredService<IFixtureRepository>();
        var venueRepository = scope.ServiceProvider.GetRequiredService<IVenueRepository>();
        var sportRepository = scope.ServiceProvider.GetRequiredService<ISportRepository>();
        var db = scope.ServiceProvider.GetRequiredService<FixturesDbContext>();

        var sport = Sport.Create("Football", "provider", "sport-batch").Value;
        var venue1 = Venue.CreateFromProvider("Stadium 1", "provider", "venue-a").Value;
        var venue2 = Venue.CreateFromProvider("Stadium 2", "provider", "venue-b").Value;
        var venueOther = Venue.CreateFromProvider("Other Stadium", "other-provider", "venue-a").Value;
        var fixture1 = Fixture.CreateFromProvider(
            sport.Id,
            null,
            null,
            "Match 1",
            new DateTimeOffset(2025, 9, 14, 15, 0, 0, TimeSpan.Zero),
            "provider",
            "fixture-a").Value;
        var fixture2 = Fixture.CreateFromProvider(
            sport.Id,
            null,
            null,
            "Match 2",
            new DateTimeOffset(2025, 9, 15, 15, 0, 0, TimeSpan.Zero),
            "provider",
            "fixture-b").Value;
        var fixtureOther = Fixture.CreateFromProvider(
            sport.Id,
            null,
            null,
            "Other Match",
            new DateTimeOffset(2025, 9, 16, 15, 0, 0, TimeSpan.Zero),
            "other-provider",
            "fixture-a").Value;

        sportRepository.Add(sport);
        venueRepository.Add(venue1);
        venueRepository.Add(venue2);
        venueRepository.Add(venueOther);
        fixtureRepository.Add(fixture1);
        fixtureRepository.Add(fixture2);
        fixtureRepository.Add(fixtureOther);
        await db.SaveChangesAsync();

        var venues = await venueRepository.GetByExternalProviderIdsAsync(
            "provider",
            ["venue-a", "venue-b", "missing-venue"]);
        venues.Select(venue => venue.ExternalProviderId).Should().BeEquivalentTo("venue-a", "venue-b");

        var fixtures = await fixtureRepository.GetByExternalProviderIdsAsync(
            "provider",
            ["fixture-a", "fixture-b", "missing-fixture"]);
        fixtures.Select(fixture => fixture.ExternalProviderId).Should().BeEquivalentTo("fixture-a", "fixture-b");

        (await venueRepository.GetByExternalProviderIdsAsync("provider", [])).Should().BeEmpty();
        (await fixtureRepository.GetByExternalProviderIdsAsync("provider", [])).Should().BeEmpty();
    }
}
