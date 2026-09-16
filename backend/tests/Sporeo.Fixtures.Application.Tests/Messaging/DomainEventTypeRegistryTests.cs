using FluentAssertions;
using Sporeo.BuildingBlocks.Application.Abstractions.Messaging;
using Sporeo.BuildingBlocks.Domain.Time;
using Sporeo.BuildingBlocks.Infrastructure.Outbox;
using Sporeo.Fixtures.Application;
using Sporeo.Fixtures.Domain.Venues.Events;

namespace Sporeo.Fixtures.Application.Tests.Messaging;

public sealed class DomainEventTypeRegistryTests
{
    [Fact]
    public void GetTypeKey_ForRegisteredVenueEvent_ShouldReturnStableKey()
    {
        var registry = new DomainEventTypeRegistry(
        [
            new KeyValuePair<string, Type>(
                DependencyInjection.VenueCreatedDomainEventTypeKey,
                typeof(VenueCreatedDomainEvent))
        ]);

        var domainEvent = new VenueCreatedDomainEvent(Guid.NewGuid());

        registry.GetTypeKey(domainEvent).Should().Be(DependencyInjection.VenueCreatedDomainEventTypeKey);
        registry.TryResolve(DependencyInjection.VenueCreatedDomainEventTypeKey, out var type).Should().BeTrue();
        type.Should().Be(typeof(VenueCreatedDomainEvent));
    }

    [Fact]
    public void OutboxMessage_DeadLetterAndReplay_ShouldResetPendingState()
    {
        var message = new OutboxMessage(Guid.NewGuid(), "type", "{}", SystemTimeProvider.Now);

        message.MarkAsFailed("boom", SystemTimeProvider.Now.AddMinutes(1));
        message.MarkAsDeadLetter("poison");

        message.Status.Should().Be(OutboxMessageStatus.DeadLetter);
        message.RetryCount.Should().Be(2);

        message.RequeueForReplay();

        message.Status.Should().Be(OutboxMessageStatus.Pending);
        message.RetryCount.Should().Be(0);
        message.Error.Should().BeNull();
        message.NextAttempt.Should().BeNull();
    }
}
