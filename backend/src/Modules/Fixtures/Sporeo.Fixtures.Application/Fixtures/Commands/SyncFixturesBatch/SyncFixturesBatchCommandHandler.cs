using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
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
    SyncChunkIsolationExecutor chunkExecutor,
    IServiceScopeFactory scopeFactory,
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

        var seasonMapResult = await EnsureSeasonsAsync(
            league.Id,
            incomingSeasonNames,
            firstUpcomingFixture?.StartDate,
            firstUpcomingFixture?.SeasonName,
            cancellationToken);

        if (seasonMapResult.IsFailure)
            return Result.Failure<SyncBatchResultDto>(seasonMapResult.Error);

        var seasonMap = seasonMapResult.Value;
        var aggregate = SyncBatchResultDto.Empty;

        foreach (var chunk in request.Fixtures.Chunk(SyncFixturesBatchCommand.ChunkSize))
        {
            try
            {
                var chunkReport = await chunkExecutor.ProcessChunkWithIsolationAsync(
                    league.SportId,
                    league.Id,
                    seasonMap,
                    request,
                    chunk.ToList(),
                    cancellationToken);

                aggregate = aggregate.Add(chunkReport);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Unhandled failure while syncing a chunk of {Count} fixtures for league {ProviderName}/{ExternalLeagueId}.",
                    chunk.Length,
                    request.ProviderName,
                    request.ExternalLeagueId);

                aggregate = aggregate.Add(
                    SyncBatchResultDto.Create(0, 0, skipped: 0, failed: chunk.Length));
            }
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

    private async Task<Result<IReadOnlyDictionary<string, SeasonId>>> EnsureSeasonsAsync(
        LeagueId leagueId,
        IReadOnlyList<string> seasonNames,
        DateTimeOffset? nextFixtureDate,
        string? nextFixtureSeasonName,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        return await sender.Send(
            new EnsureSeasonsForSyncCommand(
                leagueId,
                seasonNames,
                nextFixtureDate,
                nextFixtureSeasonName),
            cancellationToken);
    }
}
