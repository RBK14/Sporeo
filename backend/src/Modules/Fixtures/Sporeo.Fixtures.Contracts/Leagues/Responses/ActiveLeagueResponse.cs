namespace Sporeo.Fixtures.Contracts.Leagues.Responses;

/// <summary>
/// Actively monitored league with its sport and optional current season.
/// </summary>
/// <param name="Id">The league identifier.</param>
/// <param name="Name">The league display name.</param>
/// <param name="Country">The league country, if specified.</param>
/// <param name="Sport">The sport to which the league belongs.</param>
/// <param name="CurrentSeason">The current season, or null when no season is marked current.</param>
public sealed record ActiveLeagueResponse(
    Guid Id,
    string Name,
    string? Country,
    ActiveLeagueSportResponse Sport,
    ActiveLeagueSeasonResponse? CurrentSeason);

/// <summary>
/// Sport summary included in an active league.
/// </summary>
/// <param name="Id">The sport identifier.</param>
/// <param name="Name">The sport display name.</param>
public sealed record ActiveLeagueSportResponse(Guid Id, string Name);

/// <summary>
/// Current season summary included in an active league.
/// </summary>
/// <param name="Id">The season identifier.</param>
/// <param name="Name">The season display name.</param>
public sealed record ActiveLeagueSeasonResponse(Guid Id, string Name);
