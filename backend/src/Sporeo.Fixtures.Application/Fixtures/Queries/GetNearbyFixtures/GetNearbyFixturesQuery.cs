using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.Fixtures.Application.Fixtures.Queries.Common;

namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetNearbyFixtures;

public sealed record GetNearbyFixturesQuery(
    double Latitude,
    double Longitude,
    double RadiusInMeters,
    PaginationParams Pagination,
    FixtureFilters Filters) : IQuery<PagedResult<NearbyFixtureListItemResponse>>;
