namespace Sporeo.Fixtures.Contracts.Sports.Responses;

/// <summary>
/// Sport that currently has at least one monitored league.
/// </summary>
/// <param name="Id">The sport identifier.</param>
/// <param name="Name">The sport display name.</param>
public sealed record ActiveSportResponse(Guid Id, string Name);
