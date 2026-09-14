namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetNearbyFixtures;

/// <summary>
/// Summary item returned when listing fixtures near a geographic point.
/// </summary>
/// <param name="Id">The fixture identifier.</param>
/// <param name="Name">The fixture display name.</param>
/// <param name="StartDate">The scheduled start date and time.</param>
/// <param name="SportName">The display name of the fixture sport.</param>
/// <param name="LeagueName">The display name of the fixture league, if assigned.</param>
/// <param name="DistanceInMeters">The distance in meters from the search origin to the fixture venue.</param>
public sealed record NearbyFixtureListItemResponse(
    Guid Id,
    string Name,
    DateTimeOffset StartDate,
    string SportName,
    string? LeagueName,
    double DistanceInMeters);
