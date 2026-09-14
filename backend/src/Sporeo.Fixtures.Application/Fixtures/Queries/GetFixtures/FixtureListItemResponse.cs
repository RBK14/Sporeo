namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtures;

/// <summary>
/// Summary item returned when listing fixtures.
/// </summary>
/// <param name="Id">The fixture identifier.</param>
/// <param name="Name">The fixture display name.</param>
/// <param name="StartDate">The scheduled start date and time.</param>
/// <param name="SportName">The display name of the fixture sport.</param>
/// <param name="LeagueName">The display name of the fixture league, if assigned.</param>
public sealed record FixtureListItemResponse(
    Guid Id,
    string Name,
    DateTimeOffset StartDate,
    string SportName,
    string? LeagueName);
