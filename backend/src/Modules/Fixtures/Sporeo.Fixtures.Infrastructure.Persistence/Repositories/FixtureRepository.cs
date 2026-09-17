using Microsoft.EntityFrameworkCore;
using Sporeo.Fixtures.Infrastructure.Persistence.Context;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.Repositories;
using Sporeo.Fixtures.Domain.Fixtures;
using Sporeo.Fixtures.Domain.Fixtures.ValueObjects;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Repositories;

internal sealed class FixtureRepository(FixturesDbContext dbContext) : IFixtureRepository
{
    private const int ProviderIdChunkSize = 500;

    public Task<Fixture?> GetByIdAsync(FixtureId id, CancellationToken cancellationToken = default) =>
        dbContext.Fixtures.SingleOrDefaultAsync(fixture => fixture.Id == id, cancellationToken);

    public Task<Fixture?> GetByExternalProviderAsync(
        string providerName,
        string providerId,
        CancellationToken cancellationToken = default) =>
        dbContext.Fixtures.SingleOrDefaultAsync(
            fixture => fixture.ExternalProviderName == providerName
                && fixture.ExternalProviderId == providerId,
            cancellationToken);

    public async Task<IReadOnlyList<Fixture>> GetByExternalProviderIdsAsync(
        string providerName,
        IEnumerable<string> providerIds,
        CancellationToken cancellationToken = default)
    {
        var ids = providerIds as IList<string> ?? providerIds.ToList();
        if (ids.Count == 0)
            return [];

        var results = new List<Fixture>();
        foreach (var chunk in ids.Chunk(ProviderIdChunkSize))
        {
            var chunkIds = chunk.ToArray();
            var fixtures = await dbContext.Fixtures
                .Where(fixture => fixture.ExternalProviderName == providerName
                    && fixture.ExternalProviderId != null
                    && chunkIds.Contains(fixture.ExternalProviderId))
                .ToListAsync(cancellationToken);

            results.AddRange(fixtures);
        }

        return results;
    }

    public void Add(Fixture fixture) => dbContext.Fixtures.Add(fixture);
}
