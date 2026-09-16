using Microsoft.Extensions.Logging;
using MediatR;
using Quartz;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.Providers;
using Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;
using Sporeo.Fixtures.Worker.Observability;

namespace Sporeo.Fixtures.Worker.Jobs.Fixtures;

/// <summary>
/// Quartz job that fetches fixtures from an external provider and dispatches a batch sync command.
/// </summary>
/// <remarks>
/// Quartz creates a DI scope per firing. This job must not store scoped dependencies in static state.
/// Concurrent execution of the same JobKey is disallowed.
/// Chunking, scope isolation, and unique-constraint retries are handled by the application command.
/// </remarks>
[DisallowConcurrentExecution]
internal sealed class SyncFixturesJob(
    IEnumerable<IExternalFixturesClient> clients,
    ISender sender,
    ILogger<SyncFixturesJob> logger) : IJob
{
    /// <inheritdoc />
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        var dataMap = context.MergedJobDataMap;

        var syncMode = Enum.Parse<SyncMode>(dataMap.GetString("SyncMode")!);
        var providerName = dataMap.GetString("ProviderName")!;
        var externalLeagueId = dataMap.GetString("ExternalLeagueId")!;
        var externalSeasonId = dataMap.GetString("ExternalSeasonId");
        var sportId = Guid.Parse(dataMap.GetString("SportId")!);
        var leagueId = ParseOptionalGuid(dataMap.GetString("LeagueId"));
        var seasonId = ParseOptionalGuid(dataMap.GetString("SeasonId"));

        var client = clients.FirstOrDefault(c =>
            string.Equals(c.ProviderName, providerName, StringComparison.OrdinalIgnoreCase));

        if (client is null)
        {
            logger.LogError("No client found for provider: {Provider}", providerName);
            throw new JobExecutionException($"No client found for provider '{providerName}'.");
        }

        try
        {
            logger.LogInformation(
                "Starting {SyncMode} sync for league {LeagueId} via {Provider}",
                syncMode,
                externalLeagueId,
                providerName);

            var fetchResult = await client.FetchFixturesAsync(
                externalLeagueId,
                string.IsNullOrWhiteSpace(externalSeasonId) ? null : externalSeasonId,
                syncMode,
                cancellationToken);

            if (fetchResult.IsFailure)
            {
                logger.LogError(
                    "Provider {Provider} failed for league {LeagueId}: {ErrorCode} {ErrorMessage}",
                    providerName,
                    externalLeagueId,
                    fetchResult.Error.Code,
                    fetchResult.Error.Message);

                throw new JobExecutionException(
                    $"Provider '{providerName}' failed: {fetchResult.Error.Code}")
                {
                    RefireImmediately = IsRetriable(fetchResult.Error)
                };
            }

            var fixtures = fetchResult.Value;
            if (fixtures.Count == 0)
            {
                logger.LogInformation(
                    "External API returned no fixtures for league {LeagueId} (empty success).",
                    externalLeagueId);
                FixturesWorkerMetrics.SyncJobsCompleted.Add(1);
                return;
            }

            FixturesWorkerMetrics.FixturesFetched.Add(fixtures.Count);

            var command = new SyncFixturesBatchCommand(
                sportId,
                leagueId,
                seasonId,
                providerName,
                fixtures);

            var result = await sender.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                logger.LogError(
                    "During sync of league {LeagueId}, an error occurred: {ErrorCode} {ErrorMessage}",
                    externalLeagueId,
                    result.Error.Code,
                    result.Error.Message);

                throw new JobExecutionException($"Sync failed: {result.Error.Code}");
            }

            var report = result.Value;
            if (report.HasWarnings)
            {
                logger.LogWarning(
                    "Sync of league {LeagueId} completed with partial success. Status={Status}, Failed={Failed}, Inserted={Inserted}, Updated={Updated}, Skipped={Skipped}",
                    externalLeagueId,
                    report.Status,
                    report.Failed,
                    report.Inserted,
                    report.Updated,
                    report.Skipped);
            }
            else
            {
                logger.LogInformation(
                    "Successfully synchronized {Count} fixtures for league {LeagueId}. Inserted={Inserted}, Updated={Updated}",
                    fixtures.Count,
                    externalLeagueId,
                    report.Inserted,
                    report.Updated);
            }

            FixturesWorkerMetrics.SyncJobsCompleted.Add(1);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (JobExecutionException)
        {
            FixturesWorkerMetrics.SyncJobsFailed.Add(1);
            throw;
        }
        catch (Exception ex)
        {
            FixturesWorkerMetrics.SyncJobsFailed.Add(1);
            logger.LogError(ex, "Critical error during sync of league {LeagueId}", externalLeagueId);
            throw new JobExecutionException(ex);
        }
    }

    private static Guid? ParseOptionalGuid(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Guid.Parse(value);

    private static bool IsRetriable(Error error) =>
        error.Code is "ExternalFixtures.Transient" or "ExternalFixtures.RateLimited";
}
