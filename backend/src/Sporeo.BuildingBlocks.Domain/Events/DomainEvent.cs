using Sporeo.BuildingBlocks.Domain.Time;

namespace Sporeo.BuildingBlocks.Domain.Events;

public abstract record DomainEvent(Guid EventId, DateTimeOffset OccurredOn) : IDomainEvent
{
    protected DomainEvent() : this(Guid.NewGuid(), SystemTimeProvider.Now)
    {
    }
}
