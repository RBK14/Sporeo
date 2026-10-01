namespace Sporeo.Fixtures.Contracts.Sports.Responses;

/// <summary>
/// Response returned after successfully creating a sport.
/// </summary>
/// <param name="Id">The identifier of the newly created sport.</param>
public sealed record CreateSportResponse(Guid Id);
