using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.Fixtures.Application.Fixtures.Queries.Common;

namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtures;

/// <summary>
/// Query that retrieves a paginated list of fixtures matching optional filters.
/// </summary>
/// <param name="Pagination">Paging parameters for the result set.</param>
/// <param name="Filters">Optional filters applied to the fixture list.</param>
public sealed record GetFixturesQuery(
    PaginationParams Pagination,
    FixtureFilters Filters) : IQuery<PagedResult<FixtureListItemResponse>>;
