using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sporeo.Fixtures.Application.Abstractions;
using Sporeo.Fixtures.Application.Fixtures.Abstractions;
using Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatchChunk;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;

/// <summary>
/// Executes chunk sync commands in isolated DI scopes with unique-constraint retry and per-item fallback.
/// </summary>
internal sealed class SyncChunkIsolationExecutor(
    IServiceScopeFactory scopeFactory,
    IDatabaseExceptionClassifier exceptionClassifier,
    ILogger logger)
{
    private const int MaxUniqueViolationRetries = 3;

    public Task<SyncBatchResultDto> ProcessChunkWithIsolationAsync(
        SportId sportId,
        LeagueId leagueId,
        IReadOnlyDictionary<string, SeasonId> seasonMap,
        SyncFixturesBatchCommand request,
        IReadOnlyList<ExternalFixtureDto> chunk,
        CancellationToken cancellationToken) =>
        ProcessWithRetriesAsync(
            sportId,
            leagueId,
            seasonMap,
            request,
            chunk,
            fallbackToPerItem: true,
            cancellationToken);

    private async Task<SyncBatchResultDto> ProcessWithRetriesAsync(
        SportId sportId,
        LeagueId leagueId,
        IReadOnlyDictionary<string, SeasonId> seasonMap,
        SyncFixturesBatchCommand request,
        IReadOnlyList<ExternalFixtureDto> chunk,
        bool fallbackToPerItem,
        CancellationToken cancellationToken)
    {
        Exception? lastUniqueViolation = null;

        for (var attempt = 1; attempt <= MaxUniqueViolationRetries; attempt++)
        {
            try
            {
                return await SendChunkAsync(sportId, leagueId, seasonMap, request, chunk, cancellationToken);
            }
            catch (Exception ex) when (exceptionClassifier.IsUniqueConstraintViolation(ex))
            {
                lastUniqueViolation = ex;
                logger.LogWarning(
                    ex,
                    "Unique constraint conflict while syncing chunk of {Count} fixtures (attempt {Attempt}/{MaxAttempts}).",
                    chunk.Count,
                    attempt,
                    MaxUniqueViolationRetries);
            }
        }

        if (!fallbackToPerItem)
        {
            throw lastUniqueViolation ??
                  new InvalidOperationException("Unique constraint retries exhausted without capturing an exception.");
        }

        logger.LogWarning(
            lastUniqueViolation,
            "Chunk unique-constraint retries exhausted. Falling back to per-item isolation for {Count} fixtures.",
            chunk.Count);

        return await ProcessItemsIsolatedAsync(sportId, leagueId, seasonMap, request, chunk, cancellationToken);
    }

    private async Task<SyncBatchResultDto> ProcessItemsIsolatedAsync(
        SportId sportId,
        LeagueId leagueId,
        IReadOnlyDictionary<string, SeasonId> seasonMap,
        SyncFixturesBatchCommand request,
        IReadOnlyList<ExternalFixtureDto> chunk,
        CancellationToken cancellationToken)
    {
        var aggregate = SyncBatchResultDto.Empty;

        foreach (var fixture in chunk)
        {
            try
            {
                var itemReport = await ProcessWithRetriesAsync(
                    sportId,
                    leagueId,
                    seasonMap,
                    request,
                    [fixture],
                    fallbackToPerItem: false,
                    cancellationToken);

                aggregate = aggregate.Add(itemReport);
            }
            catch (Exception ex) when (exceptionClassifier.IsUniqueConstraintViolation(ex))
            {
                logger.LogWarning(
                    ex,
                    "Skipping fixture {ProviderName}/{ExternalId} after unique constraint conflict.",
                    request.ProviderName,
                    fixture.ExternalId);

                aggregate = aggregate.Add(SyncBatchResultDto.Create(0, 0, skipped: 1, failed: 0));
            }
        }

        return aggregate;
    }

    private async Task<SyncBatchResultDto> SendChunkAsync(
        SportId sportId,
        LeagueId leagueId,
        IReadOnlyDictionary<string, SeasonId> seasonMap,
        SyncFixturesBatchCommand request,
        IReadOnlyList<ExternalFixtureDto> chunk,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var command = new SyncFixturesBatchChunkCommand(
            request.ProviderName,
            sportId,
            leagueId,
            seasonMap,
            chunk);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Chunk sync failed: {result.Error.Code} {result.Error.Message}");
        }

        return result.Value;
    }
}
