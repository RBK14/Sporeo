using Sporeo.BuildingBlocks.Application.Abstractions.Execution;

namespace Sporeo.Fixtures.Application.Leagues.Commands.CreateLeague;

/// <summary>
/// Creates a league aggregate.
/// </summary>
/// <param name="SportId">The parent sport identifier.</param>
/// <param name="Name">The league display name.</param>
/// <param name="Country">Optional country association.</param>
public sealed record CreateLeagueCommand(
    Guid SportId,
    string Name,
    string? Country) : ICommand<Guid>;
