using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.Fixtures.Application.Fixtures.Queries.Common;

namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetNearbyFixtures;

/// <summary>
/// Query that retrieves a paginated list of fixtures near a geographic point.
/// </summary>
/// <param name="Latitude">The latitude of the search origin in decimal degrees.</param>
/// <param name="Longitude">The longitude of the search origin in decimal degrees.</param>
/// <param name="RadiusInMeters">The search radius in meters. Must be greater than zero.</param>
/// <param name="Pagination">Paging parameters for the result set.</param>
/// <param name="Filters">Optional filters applied to the nearby fixture list.</param>
public sealed record GetNearbyFixturesQuery(
    double Latitude,
    double Longitude,
    double RadiusInMeters,
    PaginationParams Pagination,
    FixtureFilters Filters) : IQuery<PagedResult<NearbyFixtureListItemResponse>>;
