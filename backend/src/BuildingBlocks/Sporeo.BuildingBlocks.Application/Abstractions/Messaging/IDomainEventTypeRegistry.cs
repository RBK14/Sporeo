using Sporeo.BuildingBlocks.Domain.Events;

namespace Sporeo.BuildingBlocks.Application.Abstractions.Messaging;

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

/// <summary>
/// In-memory domain event type registry.
/// </summary>
public sealed class DomainEventTypeRegistry : IDomainEventTypeRegistry
{
    private readonly Dictionary<string, Type> _typesByKey;
    private readonly Dictionary<Type, string> _keysByType;

    /// <summary>
    /// Initializes a new registry from explicit type key mappings.
    /// </summary>
    public DomainEventTypeRegistry(IEnumerable<KeyValuePair<string, Type>> mappings)
    {
        _typesByKey = new Dictionary<string, Type>(StringComparer.Ordinal);
        _keysByType = new Dictionary<Type, string>();

        foreach (var mapping in mappings)
        {
            if (!typeof(IDomainEvent).IsAssignableFrom(mapping.Value))
                throw new ArgumentException($"Type '{mapping.Value.FullName}' is not an IDomainEvent.", nameof(mappings));

            _typesByKey[mapping.Key] = mapping.Value;
            _keysByType[mapping.Value] = mapping.Key;
        }
    }

    /// <inheritdoc />
    public string GetTypeKey(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        if (_keysByType.TryGetValue(domainEvent.GetType(), out var key))
            return key;

        throw new InvalidOperationException(
            $"Domain event type '{domainEvent.GetType().FullName}' is not registered in the outbox type registry.");
    }

    /// <inheritdoc />
    public bool TryResolve(string typeKey, out Type eventType) =>
        _typesByKey.TryGetValue(typeKey, out eventType!);
}
