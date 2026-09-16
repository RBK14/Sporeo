using MediatR;
using Sporeo.BuildingBlocks.Application.Events;
using Sporeo.Fixtures.Application.Venues.Commands.EnrichVenueLocation;
using Sporeo.Fixtures.Domain.Venues.Events;

namespace Sporeo.Fixtures.Application.Venues.Events;

/// <summary>
/// Handles the notification when a venue is created by triggering the enrichment process.
/// </summary>
internal sealed class VenueCreatedDomainEventHandler(ISender sender)
    : INotificationHandler<DomainEventNotification<VenueCreatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<VenueCreatedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var command = new EnrichVenueLocationCommand(notification.DomainEvent.VenueId);

        var statusResult = await sender.Send(command, cancellationToken);
        if (statusResult.IsFailure)
            throw new InvalidOperationException(
                $"Enrichment failed for venue {notification.DomainEvent.VenueId}: {statusResult.Error.Code} {statusResult.Error.Message}");
    }
}
