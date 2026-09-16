using Sporeo.BuildingBlocks.Application.Abstractions.Execution;

namespace Sporeo.Fixtures.Application.Seasons.Commands.CreateSeason;

/// <summary>
/// Creates a season aggregate.
/// </summary>
/// <param name="LeagueId">The parent league identifier.</param>
/// <param name="Name">The season display name.</param>
/// <param name="StartDate">The season start timestamp.</param>
/// <param name="EndDate">The season end timestamp.</param>
public sealed record CreateSeasonCommand(
    Guid LeagueId,
    string Name,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate) : ICommand<Guid>;
