using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Quartz;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Abstractions.Providers;
using Sporeo.Fixtures.Application.Fixtures.Commands;
using Sporeo.Fixtures.Domain.Fixtures.Enums;
using Sporeo.Fixtures.Worker.Jobs;
using MediatR;

namespace Sporeo.Fixtures.Worker.Tests.Jobs;

public sealed class SyncFixturesJobTests
{
    [Fact]
    public async Task Execute_WithLargeFetch_ShouldSendChunkedCommands()
    {
        var fixtures = Enumerable.Range(1, SyncFixturesBatchCommand.MaxBatchSize + 25)
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
            .Returns(Result.Success(new SyncFixturesBatchReport(1, 0, 0, 0)));

        var job = new SyncFixturesJob([client], sender, NullLogger<SyncFixturesJob>.Instance);
        var context = CreateContext();

        await job.Execute(context, CancellationToken.None);

        await sender.Received(2).Send(Arg.Any<SyncFixturesBatchCommand>(), Arg.Any<CancellationToken>());
        await sender.Received(1).Send(
            Arg.Is<SyncFixturesBatchCommand>(command => command.Fixtures.Count == SyncFixturesBatchCommand.MaxBatchSize),
            Arg.Any<CancellationToken>());
        await sender.Received(1).Send(
            Arg.Is<SyncFixturesBatchCommand>(command => command.Fixtures.Count == 25),
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
    public async Task Execute_WhenBatchHasPartialFailures_ShouldThrowJobExecutionException()
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
            .Returns(Result.Success(new SyncFixturesBatchReport(1, 0, 0, Failed: 1)));

        var job = new SyncFixturesJob([client], sender, NullLogger<SyncFixturesJob>.Instance);

        var act = async () => await job.Execute(CreateContext(), CancellationToken.None);

        await act.Should().ThrowAsync<JobExecutionException>()
            .Where(ex => ex.RefireImmediately == false);
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
