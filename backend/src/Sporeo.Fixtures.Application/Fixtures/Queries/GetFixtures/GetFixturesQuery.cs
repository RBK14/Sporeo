using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.Fixtures.Application.Fixtures.Queries.Common;

namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtures;

public sealed record GetFixturesQuery(
    PaginationParams Pagination,
    FixtureFilters Filters) : IQuery<PagedResult<FixtureListItemResponse>>;
