using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Venues.Abstractions.ReadModels;
using Sporeo.Fixtures.Domain.Common;

namespace Sporeo.Fixtures.Application.Venues.Queries.GetVenueDetails;

internal sealed class GetVenueDetailsQueryHandler(IVenueReadStore readStore)
    : IQueryHandler<GetVenueDetailsQuery, VenueDetailsResponse>
{
    public async Task<Result<VenueDetailsResponse>> Handle(
        GetVenueDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var venue = await readStore.GetVenueDetailsAsync(request.VenueId, cancellationToken);
        return venue is null
            ? Result.Failure<VenueDetailsResponse>(Errors.Venue.NotFound(request.VenueId))
            : Result.Success(venue);
    }
}
