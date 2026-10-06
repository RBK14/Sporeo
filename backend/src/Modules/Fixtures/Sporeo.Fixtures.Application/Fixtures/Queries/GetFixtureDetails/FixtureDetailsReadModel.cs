using Sporeo.Fixtures.Domain.Fixtures.Enums;
using Sporeo.Fixtures.Domain.Fixtures.ValueObjects;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

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
public sealed record FixtureDetailsReadModel(
    FixtureId Id,
    string Name,
    DateTimeOffset StartDate,
    FixtureStatus Status,
    FixtureVenueReadModel? Venue,
    FixtureSportReadModel Sport,
    FixtureLeagueReadModel? League,
    FixtureSeasonReadModel? Season);

/// <summary>
/// Sport summary included in fixture details.
/// </summary>
/// <param name="Id">The sport identifier.</param>
/// <param name="Name">The sport display name.</param>
public sealed record FixtureSportReadModel(SportId Id, string Name);

/// <summary>
/// League summary included in fixture details.
/// </summary>
/// <param name="Id">The league identifier.</param>
/// <param name="Name">The league display name.</param>
public sealed record FixtureLeagueReadModel(LeagueId Id, string Name);

/// <summary>
/// Season summary included in fixture details.
/// </summary>
/// <param name="Id">The season identifier.</param>
/// <param name="Name">The season display name.</param>
public sealed record FixtureSeasonReadModel(SeasonId Id, string Name);

/// <summary>
/// Venue summary included in fixture details without exposing the venue domain identity.
/// </summary>
/// <param name="Name">The venue display name.</param>
/// <param name="Street">The street line of the venue address, if specified.</param>
/// <param name="City">The city of the venue address, if specified.</param>
/// <param name="Country">The country of the venue address, if specified.</param>
/// <param name="Latitude">The venue latitude in decimal degrees, if specified.</param>
/// <param name="Longitude">The venue longitude in decimal degrees, if specified.</param>
public sealed record FixtureVenueReadModel(
    string Name,
    string? Street,
    string? City,
    string? Country,
    double? Latitude,
    double? Longitude);
