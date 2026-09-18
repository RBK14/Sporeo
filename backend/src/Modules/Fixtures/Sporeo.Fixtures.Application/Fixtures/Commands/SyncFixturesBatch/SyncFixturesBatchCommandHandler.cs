using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Abstractions;
using Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatchChunk;
using Sporeo.Fixtures.Application.Leagues.Data;
using Sporeo.Fixtures.Application.Seasons.Commands.EnsureSeasonsForSync;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;

namespace Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;

/// <summary>
/// Orchestrates fixture batch synchronization by processing isolated chunks in fresh DI scopes.
/// </summary>
internal sealed class SyncFixturesBatchCommandHandler(
    ILeagueRepository leagueRepository,
    IServiceScopeFactory scopeFactory,
    IDatabaseExceptionClassifier exceptionClassifier,
    ILogger<SyncFixturesBatchCommandHandler> logger)
    : ICommandHandler<SyncFixturesBatchCommand, SyncBatchResultDto>
{
    /// <inheritdoc />
    public async Task<Result<SyncBatchResultDto>> Handle(
        SyncFixturesBatchCommand request,
        CancellationToken cancellationToken)
    {
        var league = await leagueRepository.GetByExternalProviderAsync(
            request.ProviderName,
            request.ExternalLeagueId,
            cancellationToken);

        if (league is null)
            return Result.Failure<SyncBatchResultDto>(Errors.League.NotFound(request.ExternalLeagueId));

        var incomingSeasonNames = request.Fixtures
            .Select(f => f.SeasonName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .ToList();

        var firstUpcomingFixture = request.Fixtures
            .Where(f => f.StartDate >= DateTimeOffset.UtcNow)
            .MinBy(f => f.StartDate);

        var seasonMap = await EnsureSeasonsAsync(
            league.Id,
            incomingSeasonNames,
            firstUpcomingFixture?.StartDate,
            firstUpcomingFixture?.SeasonName,
            cancellationToken);

        var executor = new SyncChunkIsolationExecutor(scopeFactory, exceptionClassifier, logger);
        var aggregate = SyncBatchResultDto.Empty;

        foreach (var chunk in request.Fixtures.Chunk(SyncFixturesBatchCommand.ChunkSize))
        {
            var chunkReport = await executor.ProcessChunkWithIsolationAsync(
                league.SportId,
                league.Id,
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

    private async Task<IReadOnlyDictionary<string, SeasonId>> EnsureSeasonsAsync(
        LeagueId leagueId,
        IReadOnlyList<string> seasonNames,
        DateTimeOffset? nextFixtureDate,
        string? nextFixtureSeasonName,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(
            new EnsureSeasonsForSyncCommand(
                leagueId,
                seasonNames,
                nextFixtureDate,
                nextFixtureSeasonName),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Season ensure failed: {result.Error.Code} {result.Error.Message}");
        }

        return result.Value;
    }
}
