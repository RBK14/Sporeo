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
    /// Message was claimed by a worker and is currently being processed.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Message was processed successfully.
    /// </summary>
    Processed = 2,

    /// <summary>
    /// Message exceeded the retry budget and requires manual replay.
    /// </summary>
    DeadLetter = 3
}
