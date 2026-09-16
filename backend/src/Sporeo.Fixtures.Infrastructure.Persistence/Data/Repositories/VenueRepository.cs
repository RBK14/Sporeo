using Microsoft.EntityFrameworkCore;
using Sporeo.Fixtures.Application.Abstractions.Repositories;
using Sporeo.Fixtures.Domain.Venues;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;
using Sporeo.Fixtures.Infrastructure.Persistence.Data;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Data.Repositories;

internal sealed class VenueRepository(FixturesDbContext dbContext) : IVenueRepository
{
    public Task<Venue?> GetByIdAsync(VenueId id, CancellationToken cancellationToken = default) =>
        dbContext.Venues.SingleOrDefaultAsync(venue => venue.Id == id, cancellationToken);

    public Task<Venue?> GetByExternalProviderAsync(
        string providerName,
        string providerId,
        CancellationToken cancellationToken = default) =>
        dbContext.Venues.SingleOrDefaultAsync(
            venue => venue.ExternalProviderName == providerName
                && venue.ExternalProviderId == providerId,
            cancellationToken);

    public async Task<IReadOnlyList<Venue>> GetByExternalProviderIdsAsync(
        string providerName,
        IEnumerable<string> providerIds,
        CancellationToken cancellationToken = default)
    {
        var ids = providerIds as ICollection<string> ?? providerIds.ToList();
        if (ids.Count == 0)
            return [];

        return await dbContext.Venues
            .Where(venue => venue.ExternalProviderName == providerName
                && venue.ExternalProviderId != null
                && ids.Contains(venue.ExternalProviderId))
            .ToListAsync(cancellationToken);
    }

    public void Add(Venue venue) => dbContext.Venues.Add(venue);
}
