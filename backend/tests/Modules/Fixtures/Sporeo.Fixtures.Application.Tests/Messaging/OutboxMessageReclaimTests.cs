using FluentAssertions;
using Sporeo.BuildingBlocks.Domain.Time;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Models;

namespace Sporeo.Fixtures.Application.Tests.Messaging;

public sealed class OutboxMessageReclaimTests
{
    [Fact]
    public void MarkAsReclaimed_ShouldIncrementRetryCountAndKeepProcessingStatus()
    {
        var message = new OutboxMessage(Guid.NewGuid(), "type", "{}", SystemTimeProvider.Now);
        var leaseExpiresOn = SystemTimeProvider.Now.AddMinutes(5);

        message.MarkAsProcessing(leaseExpiresOn.AddMinutes(-5));
        message.Status.Should().Be(OutboxMessageStatus.Processing);
        message.RetryCount.Should().Be(0);

        message.MarkAsReclaimed(leaseExpiresOn);

        message.Status.Should().Be(OutboxMessageStatus.Processing);
        message.RetryCount.Should().Be(1);
        message.NextAttempt.Should().Be(leaseExpiresOn);
        message.Error.Should().Contain("lease expired");
    }

    [Fact]
    public void MarkAsReclaimed_Repeatedly_ShouldAccumulateRetryCountTowardDeadLetterBudget()
    {
        var message = new OutboxMessage(Guid.NewGuid(), "type", "{}", SystemTimeProvider.Now);
        var lease = SystemTimeProvider.Now.AddMinutes(5);

        message.MarkAsProcessing(lease);
        message.MarkAsReclaimed(lease);
        message.MarkAsReclaimed(lease);

        message.RetryCount.Should().Be(2);
        (message.RetryCount + 1 >= 3).Should().BeTrue();
    }
}
