namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtures;

public sealed record FixtureListItemResponse(
    Guid Id,
    string Name,
    DateTimeOffset StartDate,
    string SportName,
    string? LeagueName);
