namespace Sporeo.Fixtures.Application.Fixtures.Queries.Common;

/// <summary>
/// Optional filters applied when listing fixtures.
/// </summary>
/// <param name="SportId">When set, only fixtures for the specified sport are returned.</param>
/// <param name="LeagueId">When set, only fixtures for the specified league are returned.</param>
/// <param name="DateFrom">When set, only fixtures that start on or after this instant are returned.</param>
/// <param name="DateTo">When set, only fixtures that start on or before this instant are returned.</param>
public sealed record FixtureFilters(
    Guid? SportId = null,
    Guid? LeagueId = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null);
