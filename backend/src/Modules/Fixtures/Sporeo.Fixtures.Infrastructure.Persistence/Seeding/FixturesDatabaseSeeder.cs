using Microsoft.EntityFrameworkCore;
using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Seasons;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;
using Sporeo.Fixtures.Domain.Sports;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Infrastructure.Persistence.Context;
using System.Reflection;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Seeding;

internal sealed class FixturesDatabaseSeeder(FixturesDbContext dbContext)
{
    private static readonly SportId FootballId =
        SportId.FromValue(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private static readonly LeagueId PremierLeagueId =
        LeagueId.FromValue(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private static readonly SeasonId PremierLeagueSeasonId =
        SeasonId.FromValue(Guid.Parse("33333333-3333-3333-3333-333333333333"));

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var football = await dbContext.Sports
            .SingleOrDefaultAsync(sport => sport.Name == "Football", cancellationToken);

        if (football is null)
        {
            football = Sport.Create("Football").Value;
            SetFixedId(football, FootballId);
            dbContext.Sports.Add(football);
        }

        var premierLeague = await dbContext.Leagues
            .SingleOrDefaultAsync(
                league => league.SportId == football.Id && league.Name == "Premier League",
                cancellationToken);

        if (premierLeague is null)
        {
            premierLeague = League.CreateManually(football.Id, "Premier League", "England").Value;
            SetFixedId(premierLeague, PremierLeagueId);
            dbContext.Leagues.Add(premierLeague);
        }

        var season = await dbContext.Seasons.SingleOrDefaultAsync(
            season => season.LeagueId == premierLeague.Id && season.Name == "2026-2027",
            cancellationToken);

        if (season is null)
        {
            season = Season.CreateManually(
                premierLeague.Id,
                "2026-2027",
                new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2027, 5, 31, 0, 0, 0, TimeSpan.Zero)).Value;

            SetFixedId(season, PremierLeagueSeasonId);
            dbContext.Seasons.Add(season);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void SetFixedId<TId>(object entity, TId fixedId)
        where TId : notnull
    {
        var idProperty = entity.GetType().GetProperty(
            "Id",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Entity '{entity.GetType().Name}' has no Id property.");

        var setter = idProperty.GetSetMethod(nonPublic: true)
            ?? throw new InvalidOperationException($"Entity '{entity.GetType().Name}' Id property has no setter.");

        setter.Invoke(entity, [fixedId]);
    }
}
