namespace Sporeo.Fixtures.Contracts.Seasons.Requests;

/// <summary>
/// Request body for creating a season.
/// </summary>
/// <param name="LeagueId">The parent league identifier.</param>
/// <param name="Name">The season display name.</param>
public sealed record CreateSeasonRequest(Guid LeagueId, string Name);
