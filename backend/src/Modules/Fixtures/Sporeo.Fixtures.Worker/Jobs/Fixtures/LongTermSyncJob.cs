using MediatR;
using Quartz;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Fixtures.Abstractions;
using Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;
using Sporeo.Fixtures.Application.Leagues.Data;
using Sporeo.Fixtures.Worker.Observability;
using Errors = Sporeo.Fixtures.Application.Common.Errors;

namespace Sporeo.Fixtures.Worker.Jobs.Fixtures;

/// <summary>
/// Quartz job that fetches full-season fixtures for monitored leagues and syncs them in batches.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class LongTermSyncJob(
    ILeagueReadStore leagueReadStore,
    IEnumerable<IExternalFixturesClient> clients,
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

        var hadFailures = false;

        foreach (var target in targets)
        {
            try
            {
                logger.LogInformation(
                    "Fetching whole season '{Season}' for league {LeagueId} via {Provider}.",
                    target.CurrentSeasonName,
                    target.ExternalProviderId,
                    target.ExternalProviderName);

                var client = clients.FirstOrDefault(c =>
                    string.Equals(c.ProviderName, target.ExternalProviderName, StringComparison.OrdinalIgnoreCase));

                if (client is null)
                {
                    logger.LogError(
                        "No client found for provider '{Provider}' (league {LeagueId}).",
                        target.ExternalProviderName,
                        target.ExternalProviderId);
                    hadFailures = true;
                    continue;
                }

                var apiResult = await client.FetchLongTermFixturesAsync(
                    target.ExternalProviderId,
                    target.CurrentSeasonName,
                    cancellationToken);

                if (apiResult.IsFailure)
                {
                    logger.LogWarning(
                        "Failed to fetch long term fixtures for league {LeagueId}: {ErrorCode} {ErrorMessage}",
                        target.ExternalProviderId,
                        apiResult.Error.Code,
                        apiResult.Error.Message);
                    hadFailures = true;
                    continue;
                }

                if (apiResult.Value.Count == 0)
                    continue;

                FixturesWorkerMetrics.FixturesFetched.Add(apiResult.Value.Count);

                var command = new SyncFixturesBatchCommand(
                    target.ExternalProviderName,
                    target.ExternalProviderId,
                    apiResult.Value);

                var syncResult = await sender.Send(command, cancellationToken);

                if (syncResult.IsFailure)
                {
                    logger.LogError(
                        "Batch sync failed for league {LeagueId}: {ErrorCode} {ErrorMessage}",
                        target.ExternalProviderId,
                        syncResult.Error.Code,
                        syncResult.Error.Message);
                    hadFailures = true;
                    continue;
                }

                if (syncResult.Value.HasWarnings)
                {
                    logger.LogWarning(
                        "Long-term sync of league {LeagueId} completed with partial success. Failed={Failed}, Skipped={Skipped}",
                        target.ExternalProviderId,
                        syncResult.Value.Failed,
                        syncResult.Value.Skipped);
                    hadFailures = true;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                hadFailures = true;
                logger.LogError(
                    ex,
                    "Unhandled error during long-term sync of league {LeagueId}",
                    target.ExternalProviderId);
            }
        }

        if (hadFailures)
        {
            FixturesWorkerMetrics.SyncJobsFailed.Add(1);
            throw new JobExecutionException("LongTermSyncJob completed with one or more league failures.");
        }

        FixturesWorkerMetrics.SyncJobsCompleted.Add(1);
        logger.LogInformation("LongTermSyncJob successfully finished processing {Count} leagues.", targets.Count);
    }
}
