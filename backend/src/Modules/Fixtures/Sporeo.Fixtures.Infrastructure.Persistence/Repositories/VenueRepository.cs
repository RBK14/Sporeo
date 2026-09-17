using Microsoft.EntityFrameworkCore;
using Sporeo.Fixtures.Infrastructure.Persistence.Context;
using Sporeo.Fixtures.Application.Venues.Abstractions.Repositories;
using Sporeo.Fixtures.Domain.Venues;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Repositories;

internal sealed class VenueRepository(FixturesDbContext dbContext) : IVenueRepository
{
    private const int ProviderIdChunkSize = 500;

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
        var ids = providerIds as IList<string> ?? providerIds.ToList();
        if (ids.Count == 0)
            return [];

        var results = new List<Venue>();
        foreach (var chunk in ids.Chunk(ProviderIdChunkSize))
        {
            var chunkIds = chunk.ToArray();
            var venues = await dbContext.Venues
                .Where(venue => venue.ExternalProviderName == providerName
                    && venue.ExternalProviderId != null
                    && chunkIds.Contains(venue.ExternalProviderId))
                .ToListAsync(cancellationToken);

            results.AddRange(venues);
        }

        return results;
    }

    public void Add(Venue venue) => dbContext.Venues.Add(venue);
}
