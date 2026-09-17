using FluentAssertions;
using Sporeo.BuildingBlocks.Domain.Events;
using Sporeo.BuildingBlocks.Domain.Models;

namespace Sporeo.BuildingBlocks.Domain.Tests.Models;

public class AggregateRootTests
{
    private sealed record TestEvent : DomainEvent;

    private sealed class TestAggregate : AggregateRoot<Guid>
    {
        public TestAggregate(Guid id) : base(id) { }

        public void RaiseEvent() => AddDomainEvent(new TestEvent());
    }

    [Fact]
    public void AddDomainEvent_ShouldExposePendingEvents()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.RaiseEvent();

        aggregate.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemovePendingEvents()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.RaiseEvent();

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }
}
