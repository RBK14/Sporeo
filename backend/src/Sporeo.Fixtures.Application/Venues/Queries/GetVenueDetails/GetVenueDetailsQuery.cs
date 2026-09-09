using Sporeo.BuildingBlocks.Application.Abstractions.Execution;

namespace Sporeo.Fixtures.Application.Venues.Queries.GetVenueDetails;

public sealed record GetVenueDetailsQuery(Guid VenueId) : IQuery<VenueDetailsResponse>
{
}
