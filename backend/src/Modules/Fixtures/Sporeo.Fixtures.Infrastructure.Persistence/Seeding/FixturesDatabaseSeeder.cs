using Microsoft.EntityFrameworkCore;
using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Seasons;
using Sporeo.Fixtures.Domain.Sports;
using Sporeo.Fixtures.Infrastructure.Persistence.Writing;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Seeding;

internal sealed class FixturesDatabaseSeeder(FixturesDbContext dbContext)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        const string providerName = "TheSportsDB";

        var soccer = await dbContext.Sports
            .SingleOrDefaultAsync(sport =>
                sport.ExternalProviderName == providerName &&
                sport.ExternalProviderId == "102",
                cancellationToken);

        if (soccer is null)
        {
            soccer = Sport.Create("Soccer", providerName, "102").Value;
            dbContext.Sports.Add(soccer);
        }

        await dbContext.SaveChangesAsync(cancellationToken);


        var soccerLeagues = new[]
        {
            ("English Premier League", "4328"),
            ("English League Championship", "4329"),
            ("Scottish Premier League", "4330"),
            ("German Bundesliga", "4331"),
            ("Italian Serie A", "4332")
        };

        foreach (var (name, providerId) in soccerLeagues)
        {
            var league = await dbContext.Leagues
                .SingleOrDefaultAsync(l =>
                    l.ExternalProviderName == providerName &&
                    l.ExternalProviderId == providerId,
                    cancellationToken);

            var shouldMonitor = providerId is not ("4329" or "4330");

            if (league is null)
            {
                league = League.CreateFromProvider(
                    soccer.Id,
                    name,
                    null,
                    providerName,
                    providerId,
                    isMonitored: shouldMonitor).Value;
                dbContext.Leagues.Add(league);
            }

            var hasCurrentSeason = await dbContext.Seasons
                .AnyAsync(s => s.LeagueId == league.Id && s.IsCurrent, cancellationToken);

            if (!hasCurrentSeason)
            {
                var season = Season.Create(league.Id, "2026-2027").Value;
                season.MarkAsCurrent();
                dbContext.Seasons.Add(season);
            }
        }

        var motorsport = await dbContext.Sports
            .SingleOrDefaultAsync(sport =>
                sport.ExternalProviderName == providerName &&
                sport.ExternalProviderId == "103",
                cancellationToken);

        if (motorsport is null)
        {
            motorsport = Sport.Create("Motorsport", providerName, "103").Value;
            dbContext.Sports.Add(motorsport);
        }


        await dbContext.SaveChangesAsync(cancellationToken);
    }
}