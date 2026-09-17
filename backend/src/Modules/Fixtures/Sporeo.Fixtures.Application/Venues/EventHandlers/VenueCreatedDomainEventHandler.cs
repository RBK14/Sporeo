using MediatR;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Application.Messaging.DomainEvents;
using Sporeo.Fixtures.Application.Venues.Commands.EnrichVenueLocation;
using Sporeo.Fixtures.Domain.Venues.Events;

namespace Sporeo.Fixtures.Application.Venues.EventHandlers;

/// <summary>
/// Handles the notification when a venue is created by triggering the enrichment process.
/// </summary>
internal sealed class VenueCreatedDomainEventHandler(
    ISender sender,
    ILogger<VenueCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<VenueCreatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<VenueCreatedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var command = new EnrichVenueLocationCommand(notification.DomainEvent.VenueId);

        var statusResult = await sender.Send(command, cancellationToken);
        if (statusResult.IsFailure)
        {
            logger.LogWarning(
                "Could not enrich location for venue {VenueId}. Reason: {Error}",
                notification.DomainEvent.VenueId,
                statusResult.Error);

            return;
        }
    }
}
