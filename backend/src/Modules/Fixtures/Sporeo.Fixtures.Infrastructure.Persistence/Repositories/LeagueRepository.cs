using Microsoft.EntityFrameworkCore;
using Sporeo.Fixtures.Infrastructure.Persistence.Context;
using Sporeo.Fixtures.Application.Leagues.Abstractions.Repositories;
using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Repositories;

internal sealed class LeagueRepository(FixturesDbContext dbContext) : ILeagueRepository
{
    public Task<League?> GetByIdAsync(LeagueId id, CancellationToken cancellationToken = default) =>
        dbContext.Leagues.SingleOrDefaultAsync(league => league.Id == id, cancellationToken);

    public Task<League?> GetByExternalProviderAsync(
        string providerName,
        string providerId,
        CancellationToken cancellationToken = default) =>
        dbContext.Leagues.SingleOrDefaultAsync(
            league => league.ExternalProviderName == providerName
                && league.ExternalProviderId == providerId,
            cancellationToken);

    public void Add(League league) => dbContext.Leagues.Add(league);
}
