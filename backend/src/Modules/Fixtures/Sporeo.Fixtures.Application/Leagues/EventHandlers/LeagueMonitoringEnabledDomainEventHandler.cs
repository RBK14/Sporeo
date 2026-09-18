using MediatR;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Application.Messaging.DomainEvents;
using Sporeo.Fixtures.Application.Fixtures.Abstractions;
using Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;
using Sporeo.Fixtures.Application.Leagues.Data;
using Sporeo.Fixtures.Domain.Leagues.Events;

namespace Sporeo.Fixtures.Application.Leagues.EventHandlers;

internal sealed class LeagueMonitoringEnabledDomainEventHandler(
    IExternalFixturesClient externalFixturesClient,
    ILeagueReadStore leagueReadStore,
    ISender sender,
    ILogger<LeagueMonitoringEnabledDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<LeagueMonitoringEnabledDomainEvent>>
{
    public async Task Handle(DomainEventNotification<LeagueMonitoringEnabledDomainEvent> notification, CancellationToken cancellationToken)
    {
        var leagueData = await leagueReadStore.GetLeagueExternalProviderDataAsync(notification.DomainEvent.LeagueId, cancellationToken);

        if (leagueData is null || string.IsNullOrWhiteSpace(leagueData.ExternalProviderId))
        {
            logger.LogWarning("League {LeagueId} not found or missing external ID.", notification.DomainEvent.LeagueId.Value);
            return;
        }

        var fixturesResult = await externalFixturesClient.FetchShortTermFixturesAsync(
            leagueData.ExternalProviderId,
            cancellationToken);

        if (fixturesResult.IsFailure)
        {
            logger.LogWarning(
                "Could not fetch initial fixtures for league {LeagueId}. Reason: {Error}",
                leagueData.ExternalProviderId,
                fixturesResult.Error);

            throw new InvalidOperationException($"Initial sync fetch failed: {fixturesResult.Error.Code}");
        }

        if (fixturesResult.Value.Count == 0)
            return;

        var batchCommand = new SyncFixturesBatchCommand(
            leagueData.ExternalProviderName!,
            leagueData.ExternalProviderId,
            fixturesResult.Value);

        var batchResult = await sender.Send(batchCommand, cancellationToken);

        if (batchResult.IsFailure)
        {
            logger.LogWarning(
                "Could not process initial sync batch for league {LeagueId}. Reason: {Error}",
                leagueData.ExternalProviderId,
                batchResult.Error);

            throw new InvalidOperationException($"Initial batch sync failed: {batchResult.Error.Code}");
        }
    }
}
