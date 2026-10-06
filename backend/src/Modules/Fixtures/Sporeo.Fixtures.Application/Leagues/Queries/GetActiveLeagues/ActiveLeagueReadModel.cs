using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Leagues.Queries.GetActiveLeagues;

/// <summary>
/// Actively monitored league with its sport and optional current season.
/// </summary>
/// <param name="Id">The league identifier.</param>
/// <param name="Name">The league display name.</param>
/// <param name="Country">The league country, if specified.</param>
/// <param name="SportId">The sport identifier the league belongs to.</param>
/// <param name="SportName">The sport display name.</param>
/// <param name="CurrentSeasonId">The current season identifier, if one is marked current.</param>
/// <param name="CurrentSeasonName">The current season display name, if one is marked current.</param>
public sealed record ActiveLeagueReadModel(
    LeagueId Id,
    string Name,
    string? Country,
    SportId SportId,
    string SportName,
    SeasonId? CurrentSeasonId,
    string? CurrentSeasonName);
