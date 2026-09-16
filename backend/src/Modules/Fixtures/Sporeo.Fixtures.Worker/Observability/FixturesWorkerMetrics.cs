using System.Diagnostics.Metrics;

namespace Sporeo.Fixtures.Worker.Observability;

/// <summary>
/// OpenTelemetry metrics for fixture synchronization and outbox processing.
/// </summary>
public static class FixturesWorkerMetrics
{
    /// <summary>
    /// Meter name used by the fixtures worker.
    /// </summary>
    public const string MeterName = "Sporeo.Fixtures.Worker";

    private static readonly Meter Meter = new(MeterName);

    /// <summary>
    /// Counts completed sync job firings.
    /// </summary>
    public static readonly Counter<long> SyncJobsCompleted =
        Meter.CreateCounter<long>("fixtures.sync.jobs.completed");

    /// <summary>
    /// Counts failed sync job firings.
    /// </summary>
    public static readonly Counter<long> SyncJobsFailed =
        Meter.CreateCounter<long>("fixtures.sync.jobs.failed");

    /// <summary>
    /// Counts fixtures fetched from external providers.
    /// </summary>
    public static readonly Counter<long> FixturesFetched =
        Meter.CreateCounter<long>("fixtures.sync.fixtures.fetched");

    /// <summary>
    /// Counts processed outbox messages.
    /// </summary>
    public static readonly Counter<long> OutboxProcessed =
        Meter.CreateCounter<long>("fixtures.outbox.processed");

    /// <summary>
    /// Counts dead-lettered outbox messages.
    /// </summary>
    public static readonly Counter<long> OutboxDeadLettered =
        Meter.CreateCounter<long>("fixtures.outbox.dead_lettered");
}
