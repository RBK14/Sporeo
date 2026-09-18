using Sporeo.BuildingBlocks.Application.Abstractions.Execution;

namespace Sporeo.Fixtures.Application.Seasons.Commands.EnsureSeasonsForSync;

/// <summary>
/// Ensures seasons for a league exist and returns a name-to-id map for fixture sync.
/// Intended to run in an isolated DI scope so <c>CommitBehavior</c> persists seasons
/// before fixture chunks reference them.
/// </summary>
internal sealed record EnsureSeasonsForSyncCommand(
    Guid LeagueId,
    IReadOnlyList<string> SeasonNames,
    DateTimeOffset? NextFixtureDate,
    string? NextFixtureSeasonName) : ICommand<IReadOnlyDictionary<string, Guid>>;
