namespace Sporeo.Fixtures.Contracts.Sports.Requests;

/// <summary>
/// Request body for creating a sport.
/// </summary>
/// <param name="Name">The sport display name.</param>
public sealed record CreateSportRequest(string Name);
