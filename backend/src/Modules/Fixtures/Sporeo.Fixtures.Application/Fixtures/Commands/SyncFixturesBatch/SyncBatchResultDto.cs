namespace Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;

/// <summary>
/// Outcome status of a fixture batch synchronization.
/// </summary>
public enum SyncBatchStatus
{
    /// <summary>
    /// Every item was processed without domain or item-level failures.
    /// </summary>
    Succeeded = 0,

    /// <summary>
    /// At least one item was skipped or failed, but the batch completed.
    /// </summary>
    PartialSuccess = 1
}

/// <summary>
/// Aggregated report of a fixture batch synchronization attempt.
/// </summary>
/// <param name="Status">Overall batch status.</param>
/// <param name="Inserted">Number of newly inserted fixtures.</param>
/// <param name="Updated">Number of updated fixtures.</param>
/// <param name="Skipped">Number of intentionally skipped fixtures.</param>
/// <param name="Failed">Number of fixtures that failed domain validation or updates.</param>
public sealed record SyncBatchResultDto(
    SyncBatchStatus Status,
    int Inserted,
    int Updated,
    int Skipped,
    int Failed)
{
    /// <summary>
    /// Gets whether any fixture item failed during the batch.
    /// </summary>
    public bool HasFailures => Failed > 0;

    /// <summary>
    /// Gets whether the batch completed with warnings (skipped or failed items).
    /// </summary>
    public bool HasWarnings => Skipped > 0 || Failed > 0;

    /// <summary>
    /// Creates an empty successful report.
    /// </summary>
    public static SyncBatchResultDto Empty { get; } = new(SyncBatchStatus.Succeeded, 0, 0, 0, 0);

    /// <summary>
    /// Combines multiple chunk reports into a single aggregate report.
    /// </summary>
    public static SyncBatchResultDto Aggregate(IEnumerable<SyncBatchResultDto> reports)
    {
        var inserted = 0;
        var updated = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var report in reports)
        {
            inserted += report.Inserted;
            updated += report.Updated;
            skipped += report.Skipped;
            failed += report.Failed;
        }

        return Create(inserted, updated, skipped, failed);
    }

    /// <summary>
    /// Creates a report and derives <see cref="SyncBatchStatus"/> from counters.
    /// </summary>
    public static SyncBatchResultDto Create(int inserted, int updated, int skipped, int failed)
    {
        var status = skipped > 0 || failed > 0
            ? SyncBatchStatus.PartialSuccess
            : SyncBatchStatus.Succeeded;

        return new SyncBatchResultDto(status, inserted, updated, skipped, failed);
    }

    /// <summary>
    /// Adds another report's counters to this report.
    /// </summary>
    public SyncBatchResultDto Add(SyncBatchResultDto other) =>
        Create(
            Inserted + other.Inserted,
            Updated + other.Updated,
            Skipped + other.Skipped,
            Failed + other.Failed);
}
