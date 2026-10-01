using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Quartz;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Fixtures.Abstractions;
using Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;
using Sporeo.Fixtures.Application.Leagues.Data;
using Sporeo.Fixtures.Application.Leagues.ReadModels;
using Sporeo.Fixtures.Domain.Fixtures.Enums;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Worker.Jobs.Fixtures;
using MediatR;
using Errors = Sporeo.Fixtures.Application.Common.Errors;

namespace Sporeo.Fixtures.Worker.Tests.Jobs;

public sealed class SyncFixturesJobTests
{
    [Fact]
    public async Task Execute_WithLargeFetch_ShouldSendSingleBatchCommand()
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

        var client = Substitute.For<IExternalFixturesClient>();
        client.ProviderName.Returns("TheSportsDB");
        client.FetchShortTermFixturesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ExternalFixtureDto>>(fixtures));

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<SyncFixturesBatchCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(SyncBatchResultDto.Create(fixtures.Count, 0, 0, 0)));

        var job = new ShortTermSyncJob([client], sender, NullLogger<ShortTermSyncJob>.Instance);
        var context = CreateContext();

        await job.Execute(context, CancellationToken.None);

        await sender.Received(1).Send(
            Arg.Is<SyncFixturesBatchCommand>(command =>
                command.Fixtures.Count == fixtures.Count &&
                command.ProviderName == "TheSportsDB" &&
                command.ExternalLeagueId == "4328"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_WhenProviderFails_ShouldThrowJobExecutionException()
    {
        var client = Substitute.For<IExternalFixturesClient>();
        client.ProviderName.Returns("TheSportsDB");
        client.FetchShortTermFixturesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<ExternalFixtureDto>>(Errors.ExternalFixtures.Unauthorized));

        var sender = Substitute.For<ISender>();
        var job = new ShortTermSyncJob([client], sender, NullLogger<ShortTermSyncJob>.Instance);

        var act = async () => await job.Execute(CreateContext(), CancellationToken.None);

        await act.Should().ThrowAsync<JobExecutionException>();
        await sender.DidNotReceive().Send(Arg.Any<SyncFixturesBatchCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_WhenBatchHasPartialSuccess_ShouldCompleteWithoutThrowing()
    {
        var fixtures = new List<ExternalFixtureDto>
        {
            new("TheSportsDB", "1", "Home vs Away", "2025-2026", DateTimeOffset.UtcNow, FixtureStatus.Scheduled, null)
        };

        var client = Substitute.For<IExternalFixturesClient>();
        client.ProviderName.Returns("TheSportsDB");
        client.FetchShortTermFixturesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ExternalFixtureDto>>(fixtures));

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<SyncFixturesBatchCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(SyncBatchResultDto.Create(1, 0, 0, failed: 1)));

        var job = new ShortTermSyncJob([client], sender, NullLogger<ShortTermSyncJob>.Instance);

        var act = async () => await job.Execute(CreateContext(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LongTermExecute_WhenBatchOnlyHasSkips_ShouldCompleteWithoutThrowing()
    {
        var leagueReadStore = Substitute.For<ILeagueReadStore>();
        leagueReadStore.GetMonitoredLeaguesWithCurrentSeasonAsync(Arg.Any<CancellationToken>())
            .Returns(
            [
                new LeagueWithCurrentSeasonReadModel(
                    LeagueId.New(),
                    "TheSportsDB",
                    "4328",
                    "2025-2026")
            ]);

        var fixtures = new List<ExternalFixtureDto>
        {
            new("TheSportsDB", "1", "Home vs Away", "2025-2026", DateTimeOffset.UtcNow, FixtureStatus.Scheduled, null)
        };

        var client = Substitute.For<IExternalFixturesClient>();
        client.ProviderName.Returns("TheSportsDB");
        client.FetchLongTermFixturesAsync("4328", "2025-2026", Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ExternalFixtureDto>>(fixtures));

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<SyncFixturesBatchCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(SyncBatchResultDto.Create(1, 0, skipped: 2, failed: 0)));

        var job = new LongTermSyncJob(
            leagueReadStore,
            [client],
            sender,
            NullLogger<LongTermSyncJob>.Instance);

        var act = async () => await job.Execute(Substitute.For<IJobExecutionContext>(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LongTermExecute_WhenBatchHasFailures_ShouldThrowJobExecutionException()
    {
        var leagueReadStore = Substitute.For<ILeagueReadStore>();
        leagueReadStore.GetMonitoredLeaguesWithCurrentSeasonAsync(Arg.Any<CancellationToken>())
            .Returns(
            [
                new LeagueWithCurrentSeasonReadModel(
                    LeagueId.New(),
                    "TheSportsDB",
                    "4328",
                    "2025-2026")
            ]);

        var fixtures = new List<ExternalFixtureDto>
        {
            new("TheSportsDB", "1", "Home vs Away", "2025-2026", DateTimeOffset.UtcNow, FixtureStatus.Scheduled, null)
        };

        var client = Substitute.For<IExternalFixturesClient>();
        client.ProviderName.Returns("TheSportsDB");
        client.FetchLongTermFixturesAsync("4328", "2025-2026", Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ExternalFixtureDto>>(fixtures));

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<SyncFixturesBatchCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(SyncBatchResultDto.Create(1, 0, skipped: 0, failed: 1)));

        var job = new LongTermSyncJob(
            leagueReadStore,
            [client],
            sender,
            NullLogger<LongTermSyncJob>.Instance);

        var act = async () => await job.Execute(Substitute.For<IJobExecutionContext>(), CancellationToken.None);

        await act.Should().ThrowAsync<JobExecutionException>();
    }

    [Fact]
    public async Task Execute_WhenFetchReturnsEmptyList_ShouldCompleteWithoutSendingBatch()
    {
        var client = Substitute.For<IExternalFixturesClient>();
        client.ProviderName.Returns("TheSportsDB");
        client.FetchShortTermFixturesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ExternalFixtureDto>>([]));

        var sender = Substitute.For<ISender>();
        var job = new ShortTermSyncJob([client], sender, NullLogger<ShortTermSyncJob>.Instance);

        await job.Execute(CreateContext(), CancellationToken.None);

        await sender.DidNotReceive().Send(Arg.Any<SyncFixturesBatchCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_WhenFetchReturnsInvalidPayload_ShouldThrowJobExecutionException()
    {
        var client = Substitute.For<IExternalFixturesClient>();
        client.ProviderName.Returns("TheSportsDB");
        client.FetchShortTermFixturesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<ExternalFixtureDto>>(Errors.ExternalFixtures.InvalidPayload));

        var sender = Substitute.For<ISender>();
        var job = new ShortTermSyncJob([client], sender, NullLogger<ShortTermSyncJob>.Instance);

        var act = async () => await job.Execute(CreateContext(), CancellationToken.None);

        await act.Should().ThrowAsync<JobExecutionException>();
        await sender.DidNotReceive().Send(Arg.Any<SyncFixturesBatchCommand>(), Arg.Any<CancellationToken>());
    }

    private static IJobExecutionContext CreateContext()
    {
        var dataMap = new JobDataMap
        {
            ["ProviderName"] = "TheSportsDB",
            ["ExternalLeagueId"] = "4328"
        };

        var context = Substitute.For<IJobExecutionContext>();
        context.MergedJobDataMap.Returns(dataMap);
        return context;
    }
}
