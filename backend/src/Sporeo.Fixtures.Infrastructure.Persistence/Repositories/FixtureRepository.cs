using Microsoft.EntityFrameworkCore;
using Sporeo.Fixtures.Domain.Fixtures;
using Sporeo.Fixtures.Domain.Fixtures.ValueObjects;
using Sporeo.Fixtures.Infrastructure.Persistence.Contexts;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Repositories;

internal sealed class FixtureRepository(FixturesDbContext dbContext) : IFixtureRepository
{
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
        var ids = providerIds as ICollection<string> ?? providerIds.ToList();
        if (ids.Count == 0)
            return [];

        return await dbContext.Fixtures
            .Where(fixture => fixture.ExternalProviderName == providerName
                && fixture.ExternalProviderId != null
                && ids.Contains(fixture.ExternalProviderId))
            .ToListAsync(cancellationToken);
    }

    public void Add(Fixture fixture) => dbContext.Fixtures.Add(fixture);
}
