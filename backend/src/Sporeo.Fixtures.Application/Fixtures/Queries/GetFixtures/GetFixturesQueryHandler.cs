using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Abstractions.ReadModel;

namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtures;

internal sealed class GetFixturesQueryHandler(IFixtureReadStore readStore)
    : IQueryHandler<GetFixturesQuery, PagedResult<FixtureListItemResponse>>
{
    public async Task<Result<PagedResult<FixtureListItemResponse>>> Handle(
        GetFixturesQuery request,
        CancellationToken cancellationToken)
    {
        var result = await readStore.GetFixturesAsync(request.Filters, request.Pagination, cancellationToken);
        return Result.Success(result);
    }
}
