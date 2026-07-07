namespace Sporeo.BuildingBlocks.Domain.Events;

/// <summary>
/// Marks an entity that collects domain events to be dispatched after persistence.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// Gets the domain events raised by the entity that have not yet been dispatched.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Removes all pending domain events from the entity.
    /// </summary>
    void ClearDomainEvents();
}
