using Sporeo.BuildingBlocks.Domain.Events;

namespace Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Abstractions;

/// <summary>
/// Dispatches a deserialized domain event after it has been loaded from the outbox.
/// </summary>
public interface IOutboxMessageDispatcher
{
    /// <summary>
    /// Dispatches the specified domain event to in-process handlers or external transports.
    /// </summary>
    Task DispatchAsync(IDomainEvent domainEvent, Type eventType, CancellationToken cancellationToken = default);
}
