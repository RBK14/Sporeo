namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetNearbyFixtures;

public sealed record NearbyFixtureListItemResponse(
    Guid Id,
    string Name,
    DateTimeOffset StartDate,
    string SportName,
    string? LeagueName,
    double DistanceInMeters);
