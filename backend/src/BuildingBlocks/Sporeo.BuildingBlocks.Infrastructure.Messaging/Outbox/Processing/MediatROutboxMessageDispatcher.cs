using MediatR;
using Sporeo.BuildingBlocks.Application.Messaging.DomainEvents;
using Sporeo.BuildingBlocks.Domain.Events;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Abstractions;

namespace Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Processing;

/// <summary>
/// Dispatches outbox domain events to MediatR notification handlers.
/// </summary>
public sealed class MediatROutboxMessageDispatcher(IPublisher publisher) : IOutboxMessageDispatcher
{
    /// <inheritdoc />
    public async Task DispatchAsync(IDomainEvent domainEvent, Type eventType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentNullException.ThrowIfNull(eventType);

        var wrapperType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
        var notification = Activator.CreateInstance(wrapperType, domainEvent)
            ?? throw new InvalidOperationException($"Unable to create notification for '{eventType.FullName}'.");

        await publisher.Publish(notification, cancellationToken);
    }
}
