using Sporeo.Fixtures.Application.Venues.Queries.GetVenueDetails;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Application.Venues.Abstractions.ReadStores;

/// <summary>
/// Read-model port for venue detail queries.
/// </summary>
public interface IVenueReadStore
{
    /// <summary>
    /// Gets venue details by identifier.
    /// </summary>
    Task<VenueDetailsReadModel?> GetVenueDetailsAsync(
        VenueId venueId,
        CancellationToken cancellationToken = default);
}
