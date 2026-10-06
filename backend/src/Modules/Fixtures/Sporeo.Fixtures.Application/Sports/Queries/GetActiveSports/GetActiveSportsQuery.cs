using Sporeo.BuildingBlocks.Application.Abstractions.Execution;

namespace Sporeo.Fixtures.Application.Sports.Queries.GetActiveSports;

/// <summary>
/// Query that retrieves sports that currently have at least one monitored league.
/// </summary>
public sealed record GetActiveSportsQuery() : IQuery<IReadOnlyList<ActiveSportReadModel>>;
