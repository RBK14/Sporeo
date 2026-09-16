using Microsoft.EntityFrameworkCore;
using Sporeo.Fixtures.Infrastructure.Persistence.Context;
using Sporeo.Fixtures.Application.Sports.Abstractions.Repositories;
using Sporeo.Fixtures.Domain.Sports;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Repositories;

internal sealed class SportRepository(FixturesDbContext dbContext) : ISportRepository
{
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

    public void Add(Sport sport) => dbContext.Sports.Add(sport);
}
