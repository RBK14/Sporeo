using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.Providers;

namespace Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;

/// <summary>
/// Synchronizes a batch of fixtures and related venues from a single external provider.
/// </summary>
/// <param name="SportId">The Sporeo sport assigned to all fixtures in the batch.</param>
/// <param name="LeagueId">Optional Sporeo league assigned to the fixtures.</param>
/// <param name="SeasonId">Optional Sporeo season assigned to the fixtures.</param>
/// <param name="ProviderName">The external provider name shared by every fixture in the batch.</param>
/// <param name="Fixtures">The provider payloads to upsert.</param>
/// <remarks>
/// The orchestrator splits the payload into isolated chunks. Each chunk is processed in a fresh
/// DI scope with its own <c>DbContext</c> and committed independently by <c>CommitBehavior</c>.
/// Domain and item-level failures produce a partial-success report; infrastructure failures propagate.
/// </remarks>
public sealed record SyncFixturesBatchCommand(
    Guid SportId,
    Guid? LeagueId,
    Guid? SeasonId,
    string ProviderName,
    IReadOnlyList<ExternalFixtureDto> Fixtures) : ICommand<SyncBatchResultDto>
{
    /// <summary>
    /// Maximum number of fixtures accepted in a single command.
    /// </summary>
    public const int MaxBatchSize = 5_000;

    /// <summary>
    /// Number of fixtures processed per isolated chunk / unit of work.
    /// </summary>
    public const int ChunkSize = 100;
}
