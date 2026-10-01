namespace Sporeo.Fixtures.Worker.Configuration;

/// <summary>
/// Quartz scheduling options for the fixtures worker.
/// </summary>
public sealed class WorkerOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Worker";

    /// <summary>
    /// Cron expression for the short-term sync dispatcher job.
    /// </summary>
    public string DispatcherCron { get; init; } = "0 30 * * * ?";

    /// <summary>
    /// Cron expression for the long-term (full season) sync job.
    /// </summary>
    public string LongTermSyncCron { get; init; } = "0 0 3 * * ?";
}
