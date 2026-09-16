using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Abstractions.ReadModel;

namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetNearbyFixtures;

internal sealed class GetNearbyFixturesQueryHandler(IFixtureReadStore readStore)
    : IQueryHandler<GetNearbyFixturesQuery, PagedResult<NearbyFixtureListItemResponse>>
{
    public async Task<Result<PagedResult<NearbyFixtureListItemResponse>>> Handle(
        GetNearbyFixturesQuery request,
        CancellationToken cancellationToken)
    {
        var result = await readStore.GetNearbyFixturesAsync(
            request.Latitude,
            request.Longitude,
            request.RadiusInMeters,
            request.Filters,
            request.Pagination,
            cancellationToken);

        return Result.Success(result);
    }
}
