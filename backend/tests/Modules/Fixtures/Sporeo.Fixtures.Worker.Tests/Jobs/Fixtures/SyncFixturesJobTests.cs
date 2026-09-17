using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Quartz;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.Providers;
using Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;
using Sporeo.Fixtures.Domain.Fixtures.Enums;
using Sporeo.Fixtures.Worker.Jobs.Fixtures;
using MediatR;

namespace Sporeo.Fixtures.Worker.Tests.Jobs;

public sealed class SyncFixturesJobTests
{
    [Fact]
    public async Task Execute_WithLargeFetch_ShouldSendSingleBatchCommand()
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

        var client = Substitute.For<IExternalFixturesClient>();
        client.ProviderName.Returns("TheSportsDB");
        client.FetchFixturesAsync(
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<SyncMode>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ExternalFixtureDto>>(fixtures));

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<SyncFixturesBatchCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(SyncBatchResultDto.Create(fixtures.Count, 0, 0, 0)));

        var job = new SyncFixturesJob([client], sender, NullLogger<SyncFixturesJob>.Instance);
        var context = CreateContext();

        await job.Execute(context, CancellationToken.None);

        await sender.Received(1).Send(
            Arg.Is<SyncFixturesBatchCommand>(command => command.Fixtures.Count == fixtures.Count),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_WhenProviderFails_ShouldThrowJobExecutionException()
    {
        var client = Substitute.For<IExternalFixturesClient>();
        client.ProviderName.Returns("TheSportsDB");
        client.FetchFixturesAsync(
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<SyncMode>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<ExternalFixtureDto>>(
                new Error("ExternalFixtures.Unauthorized", "Denied")));

        var sender = Substitute.For<ISender>();
        var job = new SyncFixturesJob([client], sender, NullLogger<SyncFixturesJob>.Instance);

        var act = async () => await job.Execute(CreateContext(), CancellationToken.None);

        await act.Should().ThrowAsync<JobExecutionException>();
        await sender.DidNotReceive().Send(Arg.Any<SyncFixturesBatchCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_WhenBatchHasPartialSuccess_ShouldCompleteWithoutThrowing()
    {
        var fixtures = new List<ExternalFixtureDto>
        {
            new("1", "TheSportsDB", "Home vs Away", DateTimeOffset.UtcNow, FixtureStatus.Scheduled, null)
        };

        var client = Substitute.For<IExternalFixturesClient>();
        client.ProviderName.Returns("TheSportsDB");
        client.FetchFixturesAsync(
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<SyncMode>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ExternalFixtureDto>>(fixtures));

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<SyncFixturesBatchCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(SyncBatchResultDto.Create(1, 0, 0, failed: 1)));

        var job = new SyncFixturesJob([client], sender, NullLogger<SyncFixturesJob>.Instance);

        var act = async () => await job.Execute(CreateContext(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Execute_WhenFetchReturnsEmptyList_ShouldCompleteWithoutSendingBatch()
    {
        var client = Substitute.For<IExternalFixturesClient>();
        client.ProviderName.Returns("TheSportsDB");
        client.FetchFixturesAsync(
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<SyncMode>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ExternalFixtureDto>>([]));

        var sender = Substitute.For<ISender>();
        var job = new SyncFixturesJob([client], sender, NullLogger<SyncFixturesJob>.Instance);

        await job.Execute(CreateContext(), CancellationToken.None);

        await sender.DidNotReceive().Send(Arg.Any<SyncFixturesBatchCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_WhenFetchReturnsInvalidPayload_ShouldThrowJobExecutionException()
    {
        var client = Substitute.For<IExternalFixturesClient>();
        client.ProviderName.Returns("TheSportsDB");
        client.FetchFixturesAsync(
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<SyncMode>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<ExternalFixtureDto>>(
                new Error("ExternalFixtures.InvalidPayload", "Provider returned an invalid payload.")));

        var sender = Substitute.For<ISender>();
        var job = new SyncFixturesJob([client], sender, NullLogger<SyncFixturesJob>.Instance);

        var act = async () => await job.Execute(CreateContext(), CancellationToken.None);

        await act.Should().ThrowAsync<JobExecutionException>();
        await sender.DidNotReceive().Send(Arg.Any<SyncFixturesBatchCommand>(), Arg.Any<CancellationToken>());
    }

    private static IJobExecutionContext CreateContext()
    {
        var dataMap = new JobDataMap
        {
            ["SyncMode"] = SyncMode.ShortTerm.ToString(),
            ["ProviderName"] = "TheSportsDB",
            ["ExternalLeagueId"] = "4328",
            ["ExternalSeasonId"] = string.Empty,
            ["SportId"] = Guid.Parse("11111111-1111-1111-1111-111111111111").ToString(),
            ["LeagueId"] = string.Empty,
            ["SeasonId"] = string.Empty
        };

        var context = Substitute.For<IJobExecutionContext>();
        context.MergedJobDataMap.Returns(dataMap);
        return context;
    }
}
