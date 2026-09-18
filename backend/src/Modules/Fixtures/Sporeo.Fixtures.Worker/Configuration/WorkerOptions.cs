namespace Sporeo.Fixtures.Worker.Configuration;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    public string DispatcherCron { get; init; } = "0 30 * * * ?";
    public string LongTermSyncCron { get; init; } = "0 0 3 * * ?";
    public string SeasonsSyncCron { get; init; } = "0 0 2 ? * MON";
}
