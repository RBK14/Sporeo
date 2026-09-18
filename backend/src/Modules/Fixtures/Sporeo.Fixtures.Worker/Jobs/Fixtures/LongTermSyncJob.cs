using MediatR;
using Quartz;
using Sporeo.Fixtures.Application.Fixtures.Abstractions;
using Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;
using Sporeo.Fixtures.Application.Leagues.Data;

namespace Sporeo.Fixtures.Worker.Jobs.Fixtures;

[DisallowConcurrentExecution]
internal sealed class LongTermSyncJob(
    ILeagueReadStore leagueReadStore,
    IExternalFixturesClient fixturesClient,
    ISender sender,
    ILogger<LongTermSyncJob> logger) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var targets = await leagueReadStore.GetMonitoredLeaguesWithCurrentSeasonAsync(cancellationToken);

        if (targets.Count == 0)
        {
            logger.LogInformation("No monitored leagues with an active season found. LongTermSyncJob finished.");
            return;
        }

        logger.LogInformation("Found {Count} leagues for Long-Term sync.", targets.Count);

        foreach (var target in targets)
        {
            logger.LogInformation("Fetching whole season '{Season}' for league {LeagueId}.",
                target.CurrentSeasonName, target.ExternalProviderId);

            var apiResult = await fixturesClient.FetchLongTermFixturesAsync(
                target.ExternalProviderId,
                target.CurrentSeasonName,
                cancellationToken);

            if (apiResult.IsFailure)
            {
                logger.LogWarning("Failed to fetch long term fixtures for league {LeagueId}: {Error}",
                    target.ExternalProviderId, apiResult.Error.Message);
                continue;
            }

            if (apiResult.Value.Count == 0)
                continue;

            var command = new SyncFixturesBatchCommand(
                target.ExternalProviderName,
                target.ExternalProviderId,
                apiResult.Value);

            var syncResult = await sender.Send(command, cancellationToken);

            if (syncResult.IsFailure)
            {
                logger.LogError("Batch sync failed for league {LeagueId}: {Error}",
                    target.ExternalProviderId, syncResult.Error.Message);
            }
        }

        logger.LogInformation("LongTermSyncJob successfully finished processing {Count} leagues.", targets.Count);
    }
}
