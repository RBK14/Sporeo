using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;

namespace Sporeo.Fixtures.Application.Seasons.Commands.CreateSeason;

/// <summary>
/// Creates a season aggregate.
/// </summary>
/// <param name="LeagueId">The parent league identifier.</param>
/// <param name="Name">The season display name.</param>
public sealed record CreateSeasonCommand(
    LeagueId LeagueId,
    string Name) : ICommand<SeasonId>;
