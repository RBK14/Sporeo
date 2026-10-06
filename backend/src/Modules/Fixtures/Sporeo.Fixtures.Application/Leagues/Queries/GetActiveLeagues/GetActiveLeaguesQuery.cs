using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Leagues.Queries.GetActiveLeagues;

/// <summary>
/// Query that retrieves actively monitored leagues, optionally filtered by sport.
/// </summary>
/// <param name="SportId">When set, only leagues for the specified sport are returned.</param>
public sealed record GetActiveLeaguesQuery(SportId? SportId = null)
    : IQuery<IReadOnlyList<ActiveLeagueReadModel>>;
