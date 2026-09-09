namespace Sporeo.Fixtures.Application.Fixtures.Queries.Common;

public sealed record FixtureFilters(
    Guid? SportId = null,
    Guid? LeagueId = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null);
