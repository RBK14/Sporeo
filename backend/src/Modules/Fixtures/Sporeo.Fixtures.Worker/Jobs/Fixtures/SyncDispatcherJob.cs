using Quartz;
using Sporeo.Fixtures.Application.Leagues.Data;

namespace Sporeo.Fixtures.Worker.Jobs.Fixtures;

[DisallowConcurrentExecution]
internal sealed class SyncDispatcherJob(
    ILeagueReadStore leagueReadStore,
    ILogger<SyncDispatcherJob> logger) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("SyncDispatcherJob started. Looking for monitored leagues.");
        var monitoredLeagues = await leagueReadStore.GetMonitoredLeaguesForSyncAsync(cancellationToken);

        if (monitoredLeagues.Count == 0)
        {
            logger.LogInformation("No monitored leagues found. Dispatcher finished.");
            return;
        }

        logger.LogInformation("Found {Count} monitored leagues. Dispatching ShortTerm sync jobs.", monitoredLeagues.Count);

        var shortTermJobKey = new JobKey(nameof(ShortTermSyncJob), "SyncJobs");

        foreach (var league in monitoredLeagues)
        {
            var jobDataMap = new JobDataMap
            {
                { "ProviderName", league.ExternalProviderName },
                { "ExternalLeagueId", league.ExternalProviderId }
            };

            await context.Scheduler.TriggerJob(
                shortTermJobKey,
                jobDataMap,
                cancellationToken);
        }

        logger.LogInformation("SyncDispatcherJob successfully dispatched {Count} jobs.", monitoredLeagues.Count);
    }
}
