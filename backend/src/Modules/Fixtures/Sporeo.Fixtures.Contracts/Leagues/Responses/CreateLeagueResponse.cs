namespace Sporeo.Fixtures.Contracts.Leagues.Responses;

/// <summary>
/// Response returned after successfully creating a league.
/// </summary>
/// <param name="Id">The identifier of the newly created league.</param>
public sealed record CreateLeagueResponse(Guid Id);
