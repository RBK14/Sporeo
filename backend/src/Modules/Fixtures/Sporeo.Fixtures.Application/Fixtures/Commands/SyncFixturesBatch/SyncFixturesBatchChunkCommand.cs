using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.Providers;

namespace Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;

/// <summary>
/// Synchronizes a single isolated chunk of fixtures within one unit of work.
/// </summary>
internal sealed record SyncFixturesBatchChunkCommand(
    string ProviderName,
    Guid SportId,
    Guid LeagueId,
    IReadOnlyDictionary<string, Guid> SeasonMap,
    IReadOnlyList<ExternalFixtureDto> Fixtures) : ICommand<SyncBatchResultDto>;
