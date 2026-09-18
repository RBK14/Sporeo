using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Application.Venues.Queries.GetVenueDetails;

/// <summary>
/// Query that retrieves detailed information for a single venue.
/// </summary>
/// <param name="VenueId">The identifier of the venue to retrieve.</param>
public sealed record GetVenueDetailsQuery(VenueId VenueId) : IQuery<VenueDetailsReadModel>;
