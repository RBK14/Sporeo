using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Sporeo.BuildingBlocks.Domain.Time;
using Sporeo.Fixtures.Application.Leagues.Queries.GetActiveLeagues;
using Sporeo.Fixtures.Application.Sports.Queries.GetActiveSports;
using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Seasons;
using Sporeo.Fixtures.Domain.Sports;
using Sporeo.Fixtures.Infrastructure.Persistence.Writing;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Tests;

[Collection(SqlServerCollection.Name)]
public sealed class CatalogQueriesSqlServerTests
{
    private const string DatabaseName = "SporeoFixturesCatalogQueryTests";

    [Fact]
    public async Task GetActiveSports_ShouldReturnOnlySportsWithMonitoredLeagues()
    {
        await using var provider = await SqlServerTestDatabase.CreateInitializedProviderAsync(DatabaseName);
        var seed = await SeedAsync(provider);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetActiveSportsQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(sport => sport.Id == seed.SportBasketball.Id && sport.Name == "Basketball");
        result.Value.Should().Contain(sport => sport.Id == seed.SportFootball.Id && sport.Name == "Football");
        result.Value.Should().NotContain(sport => sport.Id == seed.SportNoMonitored.Id);
        result.Value.Should().NotContain(sport => sport.Id == seed.SportWithoutLeagues.Id);
        result.Value.Should().NotContain(sport => sport.Id == seed.SportDeleted.Id);

        var orderedOwnedNames = result.Value
            .Where(sport => sport.Id == seed.SportBasketball.Id || sport.Id == seed.SportFootball.Id)
            .Select(sport => sport.Name);
        orderedOwnedNames.Should().Equal("Basketball", "Football");
    }

    [Fact]
    public async Task GetActiveLeagues_ShouldFilterBySportAndExposeCurrentSeason()
    {
        await using var provider = await SqlServerTestDatabase.CreateInitializedProviderAsync($"{DatabaseName}_Leagues");
        var seed = await SeedAsync(provider);
        var sender = provider.GetRequiredService<ISender>();

        var all = await sender.Send(new GetActiveLeaguesQuery());

        all.IsSuccess.Should().BeTrue();
        all.Value.Should().Contain(league => league.Id == seed.LeagueBasketballMonitored.Id);
        all.Value.Should().Contain(league => league.Id == seed.LeagueFootballMonitoredWithSeason.Id);
        all.Value.Should().Contain(league => league.Id == seed.LeagueFootballMonitoredWithoutSeason.Id);
        all.Value.Should().Contain(league => league.Id == seed.LeagueFootballMonitoredSoftDeletedSeason.Id);
        all.Value.Should().NotContain(league => league.Id == seed.LeagueNotMonitored.Id);
        all.Value.Should().NotContain(league => league.Id == seed.LeagueSoftDeletedMonitored.Id);
        all.Value.Should().NotContain(league => league.Id == seed.LeagueDeletedSportMonitored.Id);

        var ownedOrderedIds = all.Value
            .Where(league =>
                league.Id == seed.LeagueBasketballMonitored.Id
                || league.Id == seed.LeagueFootballMonitoredWithSeason.Id
                || league.Id == seed.LeagueFootballMonitoredWithoutSeason.Id
                || league.Id == seed.LeagueFootballMonitoredSoftDeletedSeason.Id)
            .Select(league => league.Id);
        ownedOrderedIds.Should().Equal(
            seed.LeagueBasketballMonitored.Id,
            seed.LeagueFootballMonitoredWithSeason.Id,
            seed.LeagueFootballMonitoredWithoutSeason.Id,
            seed.LeagueFootballMonitoredSoftDeletedSeason.Id);

        var withSeason = all.Value.Should().ContainSingle(league =>
            league.Id == seed.LeagueFootballMonitoredWithSeason.Id).Subject;
        withSeason.SportName.Should().Be("Football");
        withSeason.CurrentSeasonId.Should().Be(seed.CurrentSeason.Id);
        withSeason.CurrentSeasonName.Should().Be("2025/26");

        var withoutSeason = all.Value.Should().ContainSingle(league =>
            league.Id == seed.LeagueFootballMonitoredWithoutSeason.Id).Subject;
        withoutSeason.CurrentSeasonId.Should().BeNull();
        withoutSeason.CurrentSeasonName.Should().BeNull();

        var softDeletedSeason = all.Value.Should().ContainSingle(league =>
            league.Id == seed.LeagueFootballMonitoredSoftDeletedSeason.Id).Subject;
        softDeletedSeason.CurrentSeasonId.Should().BeNull();
        softDeletedSeason.CurrentSeasonName.Should().BeNull();

        var bySport = await sender.Send(new GetActiveLeaguesQuery(seed.SportBasketball.Id));

        bySport.IsSuccess.Should().BeTrue();
        bySport.Value.Should().ContainSingle()
            .Which.Id.Should().Be(seed.LeagueBasketballMonitored.Id);
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
            var sportNoMonitored = Sport.Create("Hockey").Value;
            var sportWithoutLeagues = Sport.Create("Tennis").Value;
            var sportDeleted = Sport.Create("Deleted Sport").Value;

            var leagueFootballMonitoredWithSeason = League.CreateManually(sportFootball.Id, "Ekstraklasa", "PL").Value;
            leagueFootballMonitoredWithSeason.ChangeMonitoringStatus(true);

            var leagueFootballMonitoredWithoutSeason = League.CreateManually(sportFootball.Id, "I Liga", "PL").Value;
            leagueFootballMonitoredWithoutSeason.ChangeMonitoringStatus(true);

            var leagueFootballMonitoredSoftDeletedSeason = League.CreateManually(sportFootball.Id, "Puchar Polski", "PL").Value;
            leagueFootballMonitoredSoftDeletedSeason.ChangeMonitoringStatus(true);

            var leagueNotMonitored = League.CreateManually(sportNoMonitored.Id, "NHL", "US").Value;

            var leagueSoftDeletedMonitored = League.CreateManually(sportFootball.Id, "Deleted League", "PL").Value;
            leagueSoftDeletedMonitored.ChangeMonitoringStatus(true);

            var leagueBasketballMonitored = League.CreateManually(sportBasketball.Id, "PLK", "PL").Value;
            leagueBasketballMonitored.ChangeMonitoringStatus(true);

            var leagueDeletedSportMonitored = League.CreateManually(sportDeleted.Id, "Deleted Sport League", "PL").Value;
            leagueDeletedSportMonitored.ChangeMonitoringStatus(true);

            var currentSeason = Season.Create(leagueFootballMonitoredWithSeason.Id, "2025/26").Value;
            currentSeason.MarkAsCurrent();

            var softDeletedSeason = Season.Create(leagueFootballMonitoredSoftDeletedSeason.Id, "Old Cup").Value;
            softDeletedSeason.MarkAsCurrent();

            db.AddRange(
                sportFootball,
                sportBasketball,
                sportNoMonitored,
                sportWithoutLeagues,
                sportDeleted,
                leagueFootballMonitoredWithSeason,
                leagueFootballMonitoredWithoutSeason,
                leagueFootballMonitoredSoftDeletedSeason,
                leagueNotMonitored,
                leagueSoftDeletedMonitored,
                leagueBasketballMonitored,
                leagueDeletedSportMonitored,
                currentSeason,
                softDeletedSeason);

            await db.SaveChangesAsync();

            softDeletedSeason.Delete();
            leagueSoftDeletedMonitored.Delete();
            sportDeleted.Delete();
            await db.SaveChangesAsync();

            return new SeedData(
                sportFootball,
                sportBasketball,
                sportNoMonitored,
                sportWithoutLeagues,
                sportDeleted,
                leagueFootballMonitoredWithSeason,
                leagueFootballMonitoredWithoutSeason,
                leagueFootballMonitoredSoftDeletedSeason,
                leagueNotMonitored,
                leagueSoftDeletedMonitored,
                leagueBasketballMonitored,
                leagueDeletedSportMonitored,
                currentSeason);
        }
        finally
        {
            SystemTimeProvider.Provider = originalTimeProvider;
        }
    }

    private sealed record SeedData(
        Sport SportFootball,
        Sport SportBasketball,
        Sport SportNoMonitored,
        Sport SportWithoutLeagues,
        Sport SportDeleted,
        League LeagueFootballMonitoredWithSeason,
        League LeagueFootballMonitoredWithoutSeason,
        League LeagueFootballMonitoredSoftDeletedSeason,
        League LeagueNotMonitored,
        League LeagueSoftDeletedMonitored,
        League LeagueBasketballMonitored,
        League LeagueDeletedSportMonitored,
        Season CurrentSeason);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
