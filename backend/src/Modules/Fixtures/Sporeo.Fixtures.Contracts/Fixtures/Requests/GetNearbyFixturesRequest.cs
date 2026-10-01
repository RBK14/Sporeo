namespace Sporeo.Fixtures.Contracts.Fixtures.Requests;

/// <summary>
/// Query parameters for listing fixtures near a geographic point.
/// </summary>
/// <param name="Latitude">The latitude of the search origin in decimal degrees.</param>
/// <param name="Longitude">The longitude of the search origin in decimal degrees.</param>
/// <param name="RadiusInMeters">The search radius in meters. Must be greater than zero.</param>
/// <param name="PageNumber">1-based page index. Defaults to 1.</param>
/// <param name="PageSize">Number of fixtures returned per page. Defaults to 10.</param>
/// <param name="SportId">When set, only fixtures for the specified sport are returned.</param>
/// <param name="LeagueId">When set, only fixtures for the specified league are returned.</param>
/// <param name="DateFrom">When set, only fixtures that start on or after this instant are returned.</param>
/// <param name="DateTo">When set, only fixtures that start on or before this instant are returned.</param>
public sealed record GetNearbyFixturesRequest(
    double Latitude,
    double Longitude,
    double RadiusInMeters,
    int PageNumber = 1,
    int PageSize = 10,
    Guid? SportId = null,
    Guid? LeagueId = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null);
