using Sporeo.BuildingBlocks.Application.Abstractions.Execution;

namespace Sporeo.Fixtures.Application.Venues.Queries.GetVenueDetails;

/// <summary>
/// Query that retrieves detailed information for a single venue.
/// </summary>
/// <param name="VenueId">The identifier of the venue to retrieve.</param>
public sealed record GetVenueDetailsQuery(Guid VenueId) : IQuery<VenueDetailsResponse>;
