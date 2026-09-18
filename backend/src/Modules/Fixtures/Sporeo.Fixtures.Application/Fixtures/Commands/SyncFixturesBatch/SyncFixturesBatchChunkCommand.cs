using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.Providers;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;

/// <summary>
/// Synchronizes a single isolated chunk of fixtures within one unit of work.
/// </summary>
internal sealed record SyncFixturesBatchChunkCommand(
    string ProviderName,
    SportId SportId,
    LeagueId LeagueId,
    IReadOnlyDictionary<string, SeasonId> SeasonMap,
    IReadOnlyList<ExternalFixtureDto> Fixtures) : ICommand<SyncBatchResultDto>;
