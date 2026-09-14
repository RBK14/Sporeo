using Microsoft.EntityFrameworkCore;
using Sporeo.Fixtures.Domain.Seasons;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;
using Sporeo.Fixtures.Infrastructure.Persistence.Contexts;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Repositories;

internal sealed class SeasonRepository(FixturesDbContext dbContext) : ISeasonRepository
{
    public Task<Season?> GetByIdAsync(SeasonId id, CancellationToken cancellationToken = default) =>
        dbContext.Seasons.SingleOrDefaultAsync(season => season.Id == id, cancellationToken);

    public Task<Season?> GetByExternalProviderAsync(
        string providerName,
        string providerId,
        CancellationToken cancellationToken = default) =>
        dbContext.Seasons.SingleOrDefaultAsync(
            season => season.ExternalProviderName == providerName
                && season.ExternalProviderId == providerId,
            cancellationToken);

    public void Add(Season season) => dbContext.Seasons.Add(season);
}
