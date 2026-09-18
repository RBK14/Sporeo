using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Leagues.Commands.CreateLeague;

/// <summary>
/// Creates a league aggregate.
/// </summary>
/// <param name="SportId">The parent sport identifier.</param>
/// <param name="Name">The league display name.</param>
/// <param name="Country">Optional country association.</param>
public sealed record CreateLeagueCommand(
    SportId SportId,
    string Name,
    string? Country) : ICommand<LeagueId>;
