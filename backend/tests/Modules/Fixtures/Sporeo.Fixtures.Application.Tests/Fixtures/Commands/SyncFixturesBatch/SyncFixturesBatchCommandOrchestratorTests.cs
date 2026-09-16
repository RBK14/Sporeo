using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MediatR;
using NSubstitute;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Abstractions.Persistence;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.Providers;
using Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;
using Sporeo.Fixtures.Domain.Fixtures.Enums;

namespace Sporeo.Fixtures.Application.Tests.Fixtures.Commands;

public sealed class SyncFixturesBatchCommandOrchestratorTests
{
    [Fact]
    public async Task Handle_ShouldSplitIntoChunksAndAggregateReports()
    {
        var fixtures = Enumerable.Range(1, SyncFixturesBatchCommand.ChunkSize + 25)
            .Select(index => new ExternalFixtureDto(
                index.ToString(),
                "TheSportsDB",
                $"Match {index}",
                DateTimeOffset.UtcNow,
                FixtureStatus.Scheduled,
                null))
            .ToList();

        var sender = Substitute.For<ISender>();
        var callCount = 0;
        sender.Send(Arg.Any<SyncFixturesBatchChunkCommand>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                callCount++;
                var command = ci.Arg<SyncFixturesBatchChunkCommand>();
                return Result.Success(SyncBatchResultDto.Create(command.Fixtures.Count, 0, 0, 0));
            });

        var scopeFactory = CreateScopeFactory(sender);
        var classifier = Substitute.For<IDatabaseExceptionClassifier>();
        classifier.IsUniqueConstraintViolation(Arg.Any<Exception>()).Returns(false);

        var handler = new SyncFixturesBatchCommandHandler(
            scopeFactory,
            classifier,
            NullLogger<SyncFixturesBatchCommandHandler>.Instance);

        var result = await handler.Handle(
            new SyncFixturesBatchCommand(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                null,
                null,
                "TheSportsDB",
                fixtures),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        callCount.Should().Be(2);
        result.Value.Inserted.Should().Be(fixtures.Count);
        result.Value.Status.Should().Be(SyncBatchStatus.Succeeded);
    }

    [Fact]
    public async Task Handle_WhenUniqueViolation_ShouldRetryThenFallbackToPerItem()
    {
        var fixtures = new List<ExternalFixtureDto>
        {
            new("1", "TheSportsDB", "A", DateTimeOffset.UtcNow, FixtureStatus.Scheduled, null),
            new("2", "TheSportsDB", "B", DateTimeOffset.UtcNow, FixtureStatus.Scheduled, null)
        };

        var sender = Substitute.For<ISender>();
        var chunkAttempts = 0;
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

        var scopeFactory = CreateScopeFactory(sender);
        var classifier = Substitute.For<IDatabaseExceptionClassifier>();
        classifier.IsUniqueConstraintViolation(Arg.Any<Exception>()).Returns(true);

        var handler = new SyncFixturesBatchCommandHandler(
            scopeFactory,
            classifier,
            NullLogger<SyncFixturesBatchCommandHandler>.Instance);

        var result = await handler.Handle(
            new SyncFixturesBatchCommand(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                null,
                null,
                "TheSportsDB",
                fixtures),
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
            new("1", "TheSportsDB", "A", DateTimeOffset.UtcNow, FixtureStatus.Scheduled, null)
        };

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<SyncFixturesBatchChunkCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task<Result<SyncBatchResultDto>>>(_ => throw CreateUniqueViolation());

        var scopeFactory = CreateScopeFactory(sender);
        var classifier = Substitute.For<IDatabaseExceptionClassifier>();
        classifier.IsUniqueConstraintViolation(Arg.Any<Exception>()).Returns(true);

        var handler = new SyncFixturesBatchCommandHandler(
            scopeFactory,
            classifier,
            NullLogger<SyncFixturesBatchCommandHandler>.Instance);

        var result = await handler.Handle(
            new SyncFixturesBatchCommand(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                null,
                null,
                "TheSportsDB",
                fixtures),
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
            new("1", "TheSportsDB", "A", DateTimeOffset.UtcNow, FixtureStatus.Scheduled, null)
        };

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<SyncFixturesBatchChunkCommand>(), Arg.Any<CancellationToken>())
            .Returns<Task<Result<SyncBatchResultDto>>>(_ => throw new InvalidOperationException("db down"));

        var scopeFactory = CreateScopeFactory(sender);
        var classifier = Substitute.For<IDatabaseExceptionClassifier>();
        classifier.IsUniqueConstraintViolation(Arg.Any<Exception>()).Returns(false);

        var handler = new SyncFixturesBatchCommandHandler(
            scopeFactory,
            classifier,
            NullLogger<SyncFixturesBatchCommandHandler>.Instance);

        var act = async () => await handler.Handle(
            new SyncFixturesBatchCommand(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                null,
                null,
                "TheSportsDB",
                fixtures),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("db down");
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
