namespace Sporeo.Fixtures.Contracts.Leagues.Requests;

/// <summary>
/// Query parameters for listing actively monitored leagues.
/// </summary>
/// <param name="SportId">When set, only leagues for the specified sport are returned.</param>
public sealed record GetActiveLeaguesRequest(Guid? SportId = null);
