namespace Sporeo.Fixtures.Contracts.Seasons.Responses;

/// <summary>
/// Response returned after successfully creating a season.
/// </summary>
/// <param name="Id">The identifier of the newly created season.</param>
public sealed record CreateSeasonResponse(Guid Id);
