namespace Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Models;

/// <summary>
/// Processing status of a durable outbox message.
/// </summary>
public enum OutboxMessageStatus
{
    /// <summary>
    /// Message is waiting to be processed or retried.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Message was processed successfully.
    /// </summary>
    Processed = 1,

    /// <summary>
    /// Message exceeded the retry budget and requires manual replay.
    /// </summary>
    DeadLetter = 2
}
