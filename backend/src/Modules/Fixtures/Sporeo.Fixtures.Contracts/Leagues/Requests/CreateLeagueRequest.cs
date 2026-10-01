namespace Sporeo.Fixtures.Contracts.Leagues.Requests;

/// <summary>
/// Request body for creating a league.
/// </summary>
/// <param name="SportId">The parent sport identifier.</param>
/// <param name="Name">The league display name.</param>
/// <param name="Country">Optional country association.</param>
public sealed record CreateLeagueRequest(Guid SportId, string Name, string? Country);
