using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.Fixtures.Application.Abstractions.Providers;

namespace Sporeo.Fixtures.Application.Fixtures.Commands;

/// <summary>
/// Synchronizes a batch of fixtures and related venues from a single external provider.
/// </summary>
/// <param name="SportId">The Sporeo sport assigned to all fixtures in the batch.</param>
/// <param name="LeagueId">Optional Sporeo league assigned to the fixtures.</param>
/// <param name="SeasonId">Optional Sporeo season assigned to the fixtures.</param>
/// <param name="ProviderName">The external provider name shared by every fixture in the batch.</param>
/// <param name="Fixtures">The provider payloads to upsert.</param>
/// <remarks>
/// The command mutates tracked aggregates only. Persistence is performed once by <c>CommitBehavior</c>.
/// Successful items are committed even when some items fail; callers inspect the report for partial failures.
/// Duplicate provider identifiers are rejected by validation before the handler runs.
/// </remarks>
public sealed record SyncFixturesBatchCommand(
    Guid SportId,
    Guid? LeagueId,
    Guid? SeasonId,
    string ProviderName,
    IReadOnlyList<ExternalFixtureDto> Fixtures) : ICommand<SyncFixturesBatchReport>
{
    /// <summary>
    /// Maximum number of fixtures accepted in a single command to stay below SQL parameter limits.
    /// </summary>
    public const int MaxBatchSize = 500;
}

/// <summary>
/// Summary of a fixture batch synchronization attempt.
/// </summary>
/// <param name="Inserted">Number of newly inserted fixtures.</param>
/// <param name="Updated">Number of updated fixtures.</param>
/// <param name="Skipped">Number of intentionally skipped fixtures.</param>
/// <param name="Failed">Number of fixtures that failed domain validation or updates.</param>
/// <param name="VenueFailed">Number of venues that failed domain validation or updates.</param>
public sealed record SyncFixturesBatchReport(
    int Inserted,
    int Updated,
    int Skipped,
    int Failed,
    int VenueFailed = 0)
{
    /// <summary>
    /// Gets whether any fixture or venue item failed during the batch.
    /// </summary>
    public bool HasFailures => Failed > 0 || VenueFailed > 0;
}
