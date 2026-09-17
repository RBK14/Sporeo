using Sporeo.Fixtures.Application.Venues.Queries.GetVenueDetails;

namespace Sporeo.Fixtures.Application.Venues.Abstractions.ReadModels;

/// <summary>
/// Read-model port for venue detail queries.
/// </summary>
public interface IVenueReadStore
{
    /// <summary>
    /// Gets venue details by identifier.
    /// </summary>
    Task<VenueDetailsResponse?> GetVenueDetailsAsync(
        Guid venueId,
        CancellationToken cancellationToken = default);
}
