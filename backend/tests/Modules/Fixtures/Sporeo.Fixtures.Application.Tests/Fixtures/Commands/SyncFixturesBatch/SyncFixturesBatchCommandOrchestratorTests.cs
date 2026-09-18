using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MediatR;
using NSubstitute;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Abstractions.Persistence;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.Providers;
using Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;
using Sporeo.Fixtures.Application.Leagues.Abstractions.Repositories;
using Sporeo.Fixtures.Application.Seasons.Commands.EnsureSeasonsForSync;
using Sporeo.Fixtures.Domain.Fixtures.Enums;
using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Tests.Fixtures.Commands;

public sealed class SyncFixturesBatchCommandOrchestratorTests
{
    private static readonly Guid SportGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Handle_ShouldEnsureSeasonsThenSplitIntoChunksAndAggregateReports()
    {
        var fixtures = Enumerable.Range(1, SyncFixturesBatchCommand.ChunkSize + 25)
            .Select(index => new ExternalFixtureDto(
                "TheSportsDB",
                index.ToString(),
                $"Match {index}",
                "2025-2026",
                DateTimeOffset.UtcNow,
                FixtureStatus.Scheduled,
                null))
            .ToList();

        var sender = Substitute.For<ISender>();
        var chunkCallCount = 0;

        sender.Send(Arg.Any<EnsureSeasonsForSyncCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyDictionary<string, Guid>>(
                new Dictionary<string, Guid> { ["2025-2026"] = Guid.NewGuid() }));

        sender.Send(Arg.Any<SyncFixturesBatchChunkCommand>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                chunkCallCount++;
                var command = ci.Arg<SyncFixturesBatchChunkCommand>();
                return Result.Success(SyncBatchResultDto.Create(command.Fixtures.Count, 0, 0, 0));
            });

        var handler = CreateHandler(sender);

        var result = await handler.Handle(
            new SyncFixturesBatchCommand("TheSportsDB", "ext-league-1", fixtures),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        chunkCallCount.Should().Be(2);
        result.Value.Inserted.Should().Be(fixtures.Count);
        result.Value.Status.Should().Be(SyncBatchStatus.Succeeded);
        await sender.Received(1).Send(Arg.Any<EnsureSeasonsForSyncCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUniqueViolation_ShouldRetryThenFallbackToPerItem()
    {
        var fixtures = new List<ExternalFixtureDto>
        {
            new("TheSportsDB", "1", "A", "2025-2026", DateTimeOffset.UtcNow, FixtureStatus.Scheduled, null),
            new("TheSportsDB", "2", "B", "2025-2026", DateTimeOffset.UtcNow, FixtureStatus.Scheduled, null)
        };

        var sender = Substitute.For<ISender>();
        var chunkAttempts = 0;

        sender.Send(Arg.Any<EnsureSeasonsForSyncCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyDictionary<string, Guid>>(
                new Dictionary<string, Guid> { ["2025-2026"] = Guid.NewGuid() }));

        sender.Send(Arg.Any<SyncFixturesBatchChunkCommand>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var command = ci.Arg<SyncFixturesBatchChunkCommand>();
                if (command.Fixtures.Count > 1)
                {
                    chunkAttempts++;
                    throw CreateUniqueViolation();
                }

                return Result.Success(SyncBatchResultDto.Create(1, 0, 0, 0));
            });

        var classifier = Substitute.For<IDatabaseExceptionClassifier>();
        classifier.IsUniqueConstraintViolation(Arg.Any<Exception>()).Returns(true);

        var handler = CreateHandler(sender, classifier);

        var result = await handler.Handle(
            new SyncFixturesBatchCommand("TheSportsDB", "ext-league-1", fixtures),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        chunkAttempts.Should().Be(3);
        result.Value.Inserted.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenPerItemUniqueViolationExhausted_ShouldSkipItem()
    {
        var fixtures = new List<ExternalFixtureDto>
        {
            new("TheSportsDB", "1", "A", "2025-2026", DateTimeOffset.UtcNow, FixtureStatus.Scheduled, null)
        };

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<EnsureSeasonsForSyncCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyDictionary<string, Guid>>(
                new Dictionary<string, Guid> { ["2025-2026"] = Guid.NewGuid() }));

        sender.Send(Arg.Any<SyncFixturesBatchChunkCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task<Result<SyncBatchResultDto>>>(_ => throw CreateUniqueViolation());

        var classifier = Substitute.For<IDatabaseExceptionClassifier>();
        classifier.IsUniqueConstraintViolation(Arg.Any<Exception>()).Returns(true);

        var handler = CreateHandler(sender, classifier);

        var result = await handler.Handle(
            new SyncFixturesBatchCommand("TheSportsDB", "ext-league-1", fixtures),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Skipped.Should().Be(1);
        result.Value.Status.Should().Be(SyncBatchStatus.PartialSuccess);
    }

    [Fact]
    public async Task Handle_WhenInfrastructureFailure_ShouldPropagate()
    {
        var fixtures = new List<ExternalFixtureDto>
        {
            new("TheSportsDB", "1", "A", "2025-2026", DateTimeOffset.UtcNow, FixtureStatus.Scheduled, null)
        };

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<EnsureSeasonsForSyncCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyDictionary<string, Guid>>(
                new Dictionary<string, Guid> { ["2025-2026"] = Guid.NewGuid() }));

        sender.Send(Arg.Any<SyncFixturesBatchChunkCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task<Result<SyncBatchResultDto>>>(_ => throw new InvalidOperationException("db down"));

        var handler = CreateHandler(sender);

        var act = async () => await handler.Handle(
            new SyncFixturesBatchCommand("TheSportsDB", "ext-league-1", fixtures),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("db down");
    }

    [Fact]
    public async Task Handle_WhenLeagueMissing_ShouldFailWithoutEnsuringSeasons()
    {
        var fixtures = new List<ExternalFixtureDto>
        {
            new("TheSportsDB", "1", "A", "2025-2026", DateTimeOffset.UtcNow, FixtureStatus.Scheduled, null)
        };

        var sender = Substitute.For<ISender>();
        var leagueRepository = Substitute.For<ILeagueRepository>();
        leagueRepository
            .GetByExternalProviderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((League?)null);

        var handler = new SyncFixturesBatchCommandHandler(
            leagueRepository,
            CreateScopeFactory(sender),
            Substitute.For<IDatabaseExceptionClassifier>(),
            NullLogger<SyncFixturesBatchCommandHandler>.Instance);

        var result = await handler.Handle(
            new SyncFixturesBatchCommand("TheSportsDB", "missing", fixtures),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await sender.DidNotReceive().Send(Arg.Any<EnsureSeasonsForSyncCommand>(), Arg.Any<CancellationToken>());
        await sender.DidNotReceive().Send(Arg.Any<SyncFixturesBatchChunkCommand>(), Arg.Any<CancellationToken>());
    }

    private static SyncFixturesBatchCommandHandler CreateHandler(
        ISender sender,
        IDatabaseExceptionClassifier? classifier = null)
    {
        var leagueRepository = Substitute.For<ILeagueRepository>();
        var league = League.CreateFromProvider(
            SportId.FromValue(SportGuid),
            "Premier League",
            "England",
            "TheSportsDB",
            "ext-league-1").Value;

        leagueRepository
            .GetByExternalProviderAsync("TheSportsDB", "ext-league-1", Arg.Any<CancellationToken>())
            .Returns(league);

        if (classifier is null)
        {
            classifier = Substitute.For<IDatabaseExceptionClassifier>();
            classifier.IsUniqueConstraintViolation(Arg.Any<Exception>()).Returns(false);
        }

        return new SyncFixturesBatchCommandHandler(
            leagueRepository,
            CreateScopeFactory(sender),
            classifier,
            NullLogger<SyncFixturesBatchCommandHandler>.Instance);
    }

    private static IServiceScopeFactory CreateScopeFactory(ISender sender)
    {
        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(ISender)).Returns(sender);
        scope.ServiceProvider.Returns(provider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);
        return scopeFactory;
    }

    private static Exception CreateUniqueViolation() =>
        new InvalidOperationException("unique");
}
