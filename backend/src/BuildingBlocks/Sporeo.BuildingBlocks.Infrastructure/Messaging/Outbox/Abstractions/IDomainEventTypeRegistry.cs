using Sporeo.BuildingBlocks.Domain.Events;

namespace Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Abstractions;

/// <summary>
/// Versioned registry that maps durable outbox type keys to CLR domain-event types.
/// </summary>
public interface IDomainEventTypeRegistry
{
    /// <summary>
    /// Gets the durable type key for the specified domain event instance.
    /// </summary>
    string GetTypeKey(IDomainEvent domainEvent);

    /// <summary>
    /// Attempts to resolve a CLR type from a durable type key.
    /// </summary>
    bool TryResolve(string typeKey, out Type eventType);
}
