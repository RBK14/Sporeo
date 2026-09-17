using Microsoft.EntityFrameworkCore;
using Sporeo.Fixtures.Infrastructure.Persistence.Context;
using Sporeo.Fixtures.Application.Leagues.Abstractions.Repositories;
using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Repositories;

internal sealed class LeagueRepository(FixturesDbContext dbContext) : ILeagueRepository
{
    private const int ProviderIdChunkSize = 500;

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

    public async Task<IReadOnlyList<League>> GetByExternalProviderIdsAsync(
        string providerName,
        IEnumerable<string> providerIds,
        CancellationToken cancellationToken = default)
    {
        var ids = providerIds as IList<string> ?? providerIds.ToList();
        if (ids.Count == 0)
            return [];

        var results = new List<League>();
        foreach (var chunk in ids.Chunk(ProviderIdChunkSize))
        {
            var chunkIds = chunk.ToArray();
            var leagues = await dbContext.Leagues
                .Where(league => league.ExternalProviderName == providerName
                    && league.ExternalProviderId != null
                    && chunkIds.Contains(league.ExternalProviderId))
                .ToListAsync(cancellationToken);

            results.AddRange(leagues);
        }

        return results;
    }

    public void Add(League league) => dbContext.Leagues.Add(league);
}
