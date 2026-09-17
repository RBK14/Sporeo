using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.Fixtures.Application.Fixtures.Queries.Common;
using Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtureDetails;
using Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtures;
using Sporeo.Fixtures.Application.Fixtures.Queries.GetNearbyFixtures;

namespace Sporeo.Fixtures.Application.Fixtures.Abstractions.ReadModels;

/// <summary>
/// Read-model port for fixture list and detail queries.
/// </summary>
public interface IFixtureReadStore
{
    /// <summary>
    /// Queries a paged fixture list.
    /// </summary>
    Task<PagedResult<FixtureListItemResponse>> GetFixturesAsync(
        FixtureFilters filters,
        PaginationParams pagination,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries nearby fixtures ordered by distance.
    /// </summary>
    Task<PagedResult<NearbyFixtureListItemResponse>> GetNearbyFixturesAsync(
        double latitude,
        double longitude,
        double radiusInMeters,
        FixtureFilters filters,
        PaginationParams pagination,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets fixture details by identifier.
    /// </summary>
    Task<FixtureDetailsResponse?> GetFixtureDetailsAsync(
        Guid fixtureId,
        CancellationToken cancellationToken = default);
}
