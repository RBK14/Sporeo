using Microsoft.EntityFrameworkCore;
using Sporeo.Fixtures.Infrastructure.Persistence.Context;
using Sporeo.Fixtures.Application.Sports.Abstractions.Repositories;
using Sporeo.Fixtures.Domain.Sports;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Repositories;

internal sealed class SportRepository(FixturesDbContext dbContext) : ISportRepository
{
    private const int ProviderIdChunkSize = 500;

    public Task<Sport?> GetByIdAsync(SportId id, CancellationToken cancellationToken = default) =>
        dbContext.Sports.SingleOrDefaultAsync(sport => sport.Id == id, cancellationToken);

    public Task<Sport?> GetByExternalProviderAsync(
        string providerName,
        string providerId,
        CancellationToken cancellationToken = default) =>
        dbContext.Sports.SingleOrDefaultAsync(
            sport => sport.ExternalProviderName == providerName
                && sport.ExternalProviderId == providerId,
            cancellationToken);

    public async Task<IReadOnlyList<Sport>> GetByExternalProviderIdsAsync(
        string providerName,
        IEnumerable<string> providerIds,
        CancellationToken cancellationToken = default)
    {
        var ids = providerIds as IList<string> ?? providerIds.ToList();
        if (ids.Count == 0)
            return [];

        var results = new List<Sport>();
        foreach (var chunk in ids.Chunk(ProviderIdChunkSize))
        {
            var chunkIds = chunk.ToArray();
            var sports = await dbContext.Sports
                .Where(sport => sport.ExternalProviderName == providerName
                    && sport.ExternalProviderId != null
                    && chunkIds.Contains(sport.ExternalProviderId))
                .ToListAsync(cancellationToken);

            results.AddRange(sports);
        }

        return results;
    }

    public void Add(Sport sport) => dbContext.Sports.Add(sport);
}
