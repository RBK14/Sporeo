using Sporeo.BuildingBlocks.Domain.Time;

namespace Sporeo.BuildingBlocks.Domain.Events;

/// <summary>
/// Base record for domain events raised by aggregate roots.
/// </summary>
/// <param name="EventId">The unique identifier of the event occurrence.</param>
/// <param name="OccurredOn">The UTC timestamp when the event occurred.</param>
public abstract record DomainEvent(Guid EventId, DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> class
    /// with a newly generated identifier and the current UTC time.
    /// </summary>
    protected DomainEvent() : this(Guid.NewGuid(), SystemTimeProvider.Now)
    {
    }
}
