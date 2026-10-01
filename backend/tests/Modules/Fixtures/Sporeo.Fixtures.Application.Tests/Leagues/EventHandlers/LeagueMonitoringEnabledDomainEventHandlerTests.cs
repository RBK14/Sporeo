using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sporeo.BuildingBlocks.Application.Messaging.DomainEvents;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Fixtures.Abstractions;
using Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;
using Sporeo.Fixtures.Application.Leagues.Data;
using Sporeo.Fixtures.Application.Leagues.EventHandlers;
using Sporeo.Fixtures.Application.Leagues.ReadModels;
using Sporeo.Fixtures.Domain.Fixtures.Enums;
using Sporeo.Fixtures.Domain.Leagues.Events;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;

namespace Sporeo.Fixtures.Application.Tests.Leagues.EventHandlers;

public sealed class LeagueMonitoringEnabledDomainEventHandlerTests
{
    [Fact]
    public async Task Handle_WhenBatchOnlyHasSkips_ShouldNotThrow()
    {
        var leagueId = LeagueId.New();
        var handler = CreateHandler(
            leagueId,
            SyncBatchResultDto.Create(inserted: 1, updated: 0, skipped: 2, failed: 0));

        var act = async () => await handler.Handle(CreateNotification(leagueId), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_WhenBatchHasFailures_ShouldThrow()
    {
        var leagueId = LeagueId.New();
        var handler = CreateHandler(
            leagueId,
            SyncBatchResultDto.Create(inserted: 1, updated: 0, skipped: 0, failed: 1));

        var act = async () => await handler.Handle(CreateNotification(leagueId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*failures*");
    }

    private static LeagueMonitoringEnabledDomainEventHandler CreateHandler(
        LeagueId leagueId,
        SyncBatchResultDto batchResult)
    {
        var leagueReadStore = Substitute.For<ILeagueReadStore>();
        leagueReadStore.GetLeagueExternalProviderDataAsync(leagueId, Arg.Any<CancellationToken>())
            .Returns(new LeagueExternalProviderDataReadModel("TheSportsDB", "4328"));

        var fixtures = new List<ExternalFixtureDto>
        {
            new("TheSportsDB", "1", "Home vs Away", "2025-2026", DateTimeOffset.UtcNow, FixtureStatus.Scheduled, null)
        };

        var client = Substitute.For<IExternalFixturesClient>();
        client.FetchShortTermFixturesAsync("4328", Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ExternalFixtureDto>>(fixtures));

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<SyncFixturesBatchCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(batchResult));

        return new LeagueMonitoringEnabledDomainEventHandler(
            client,
            leagueReadStore,
            sender,
            NullLogger<LeagueMonitoringEnabledDomainEventHandler>.Instance);
    }

    private static DomainEventNotification<LeagueMonitoringEnabledDomainEvent> CreateNotification(LeagueId leagueId) =>
        new(new LeagueMonitoringEnabledDomainEvent(leagueId));
}
