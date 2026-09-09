using Sporeo.Fixtures.Domain.Fixtures.Enums;

namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtureDetails;

public sealed record FixtureDetailsResponse(
    Guid Id,
    string Name,
    DateTimeOffset StartDate,
    FixtureStatus Status,
    FixtureVenueDto? Venue,
    FixtureSportDto Sport,
    FixtureLeagueDto? League,
    FixtureSeasonDto? Season);

public sealed record FixtureSportDto(Guid Id, string Name);
public sealed record FixtureLeagueDto(Guid Id, string Name);
public sealed record FixtureSeasonDto(Guid Id, string Name);
public sealed record FixtureVenueDto(Guid Id, string Name, string? Street, string City, string Country);