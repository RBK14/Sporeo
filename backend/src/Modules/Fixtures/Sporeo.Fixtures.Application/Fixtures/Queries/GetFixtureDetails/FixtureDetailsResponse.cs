using Sporeo.Fixtures.Domain.Fixtures.Enums;

namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtureDetails;

/// <summary>
/// Detailed fixture data including optional venue, league, and season relations.
/// </summary>
/// <param name="Id">The fixture identifier.</param>
/// <param name="Name">The fixture display name.</param>
/// <param name="StartDate">The scheduled start date and time.</param>
/// <param name="Status">The current lifecycle status of the fixture.</param>
/// <param name="Venue">The assigned venue, or <see langword="null"/> when none is assigned or the venue is unavailable.</param>
/// <param name="Sport">The sport to which the fixture belongs.</param>
/// <param name="League">The league to which the fixture belongs, or <see langword="null"/> when none is assigned or the league is unavailable.</param>
/// <param name="Season">The season to which the fixture belongs, or <see langword="null"/> when none is assigned or the season is unavailable.</param>
public sealed record FixtureDetailsResponse(
    Guid Id,
    string Name,
    DateTimeOffset StartDate,
    FixtureStatus Status,
    FixtureVenueDto? Venue,
    FixtureSportDto Sport,
    FixtureLeagueDto? League,
    FixtureSeasonDto? Season);

/// <summary>
/// Sport summary included in fixture details.
/// </summary>
/// <param name="Id">The sport identifier.</param>
/// <param name="Name">The sport display name.</param>
public sealed record FixtureSportDto(Guid Id, string Name);

/// <summary>
/// League summary included in fixture details.
/// </summary>
/// <param name="Id">The league identifier.</param>
/// <param name="Name">The league display name.</param>
public sealed record FixtureLeagueDto(Guid Id, string Name);

/// <summary>
/// Season summary included in fixture details.
/// </summary>
/// <param name="Id">The season identifier.</param>
/// <param name="Name">The season display name.</param>
public sealed record FixtureSeasonDto(Guid Id, string Name);

/// <summary>
/// Venue summary included in fixture details.
/// </summary>
/// <param name="Id">The venue identifier.</param>
/// <param name="Name">The venue display name.</param>
/// <param name="Street">The street line of the venue address, if specified.</param>
/// <param name="City">The city of the venue address, if specified.</param>
/// <param name="Country">The country of the venue address, if specified.</param>
public sealed record FixtureVenueDto(Guid Id, string Name, string? Street, string? City, string? Country);
