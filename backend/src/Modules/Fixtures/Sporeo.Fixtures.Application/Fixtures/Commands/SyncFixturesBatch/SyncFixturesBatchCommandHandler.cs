using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Abstractions.Persistence;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.Providers;
using Sporeo.Fixtures.Application.Leagues.Abstractions.Repositories;
using Sporeo.Fixtures.Application.Seasons.Abstractions.Repositories;
using Sporeo.Fixtures.Domain.Common;

namespace Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;

/// <summary>
/// Orchestrates fixture batch synchronization by processing isolated chunks in fresh DI scopes.
/// </summary>
internal sealed class SyncFixturesBatchCommandHandler(
    ILeagueRepository leagueRepository,
    ISeasonRepository seasonRepository,
    IServiceScopeFactory scopeFactory,
    IDatabaseExceptionClassifier exceptionClassifier,
    ILogger<SyncFixturesBatchCommandHandler> logger)
    : ICommandHandler<SyncFixturesBatchCommand, SyncBatchResultDto>
{
    private const int MaxUniqueViolationRetries = 3;

    /// <inheritdoc />
    public async Task<Result<SyncBatchResultDto>> Handle(
        SyncFixturesBatchCommand request,
        CancellationToken cancellationToken)
    {
        var league = await leagueRepository.GetByExternalProviderAsync(request.ProviderName, request.ExternalLeagueId, cancellationToken);
        if (league is null)
            return Result.Failure<SyncBatchResultDto>(Errors.League.NotFound(request.ExternalLeagueId));

        var seasons = await seasonRepository.GetByLeagueIdAsync(league.Id, cancellationToken);
        var seasonMap = seasons.ToDictionary(s => s.Name, s => s.Id.Value);

        var aggregate = SyncBatchResultDto.Empty;

        foreach (var chunk in request.Fixtures.Chunk(SyncFixturesBatchCommand.ChunkSize))
        {
            var chunkReport = await ProcessChunkWithIsolationAsync(
                league.SportId.Value,
                league.Id.Value,
                seasonMap,
                request,
                chunk.ToList(),
                cancellationToken);

            aggregate = aggregate.Add(chunkReport);
        }

        if (aggregate.HasWarnings)
        {
            logger.LogWarning(
                "Fixture batch sync completed with partial success. Status={Status}, Inserted={Inserted}, Updated={Updated}, Skipped={Skipped}, Failed={Failed}",
                aggregate.Status,
                aggregate.Inserted,
                aggregate.Updated,
                aggregate.Skipped,
                aggregate.Failed);
        }
        else
        {
            logger.LogInformation(
                "Fixture batch sync succeeded. Inserted={Inserted}, Updated={Updated}",
                aggregate.Inserted,
                aggregate.Updated);
        }

        return Result.Success(aggregate);
    }

    private async Task<SyncBatchResultDto> ProcessChunkWithIsolationAsync(
        Guid sportId,
        Guid leagueId,
        IReadOnlyDictionary<string, Guid> seasonMap,
        SyncFixturesBatchCommand request,
        IReadOnlyList<ExternalFixtureDto> chunk,
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

        logger.LogWarning(
            lastUniqueViolation,
            "Chunk unique-constraint retries exhausted. Falling back to per-item isolation for {Count} fixtures.",
            chunk.Count);

        return await ProcessItemsIsolatedAsync(sportId, leagueId, seasonMap, request, chunk, cancellationToken);
    }

    private async Task<SyncBatchResultDto> ProcessItemsIsolatedAsync(
        Guid sportId,
        Guid leagueId,
        IReadOnlyDictionary<string, Guid> seasonMap,
        SyncFixturesBatchCommand request,
        IReadOnlyList<ExternalFixtureDto> chunk,
        CancellationToken cancellationToken)
    {
        var aggregate = SyncBatchResultDto.Empty;

        foreach (var fixture in chunk)
        {
            try
            {
                var itemReport = await ProcessChunkWithIsolationForSingleItemAsync(
                    sportId,
                    leagueId,
                    seasonMap,
                    request,
                    fixture,
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

    private async Task<SyncBatchResultDto> ProcessChunkWithIsolationForSingleItemAsync(
        Guid sportId,
        Guid leagueId,
        IReadOnlyDictionary<string, Guid> seasonMap,
        SyncFixturesBatchCommand request,
        ExternalFixtureDto fixture,
        CancellationToken cancellationToken)
    {
        Exception? lastUniqueViolation = null;

        for (var attempt = 1; attempt <= MaxUniqueViolationRetries; attempt++)
        {
            try
            {
                return await SendChunkAsync(sportId, leagueId, seasonMap, request, [fixture], cancellationToken);
            }
            catch (Exception ex) when (exceptionClassifier.IsUniqueConstraintViolation(ex))
            {
                lastUniqueViolation = ex;
                logger.LogWarning(
                    ex,
                    "Unique constraint conflict for fixture {ProviderName}/{ExternalId} (attempt {Attempt}/{MaxAttempts}).",
                    request.ProviderName,
                    fixture.ExternalId,
                    attempt,
                    MaxUniqueViolationRetries);
            }
        }

        throw lastUniqueViolation ??
              new InvalidOperationException("Unique constraint retries exhausted without capturing an exception.");
    }

    private async Task<SyncBatchResultDto> SendChunkAsync(
        Guid sportId,
        Guid leagueId,
        IReadOnlyDictionary<string, Guid> seasonMap,
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