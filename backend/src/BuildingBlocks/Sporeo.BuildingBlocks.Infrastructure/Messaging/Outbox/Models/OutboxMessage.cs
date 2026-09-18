namespace Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Models;

/// <summary>
/// Durable outbox message persisted together with the originating aggregate changes.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>
    /// Gets the message identifier, typically the domain event identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the versioned event type key used for deserialization.
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Gets the serialized domain event payload.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    public DateTimeOffset OccurredOn { get; init; }

    /// <summary>
    /// Gets the processing status.
    /// </summary>
    public OutboxMessageStatus Status { get; private set; } = OutboxMessageStatus.Pending;

    /// <summary>
    /// Gets the UTC timestamp when the message was processed successfully.
    /// </summary>
    public DateTimeOffset? ProcessedOn { get; private set; }

    /// <summary>
    /// Gets the last error message, if any.
    /// </summary>
    public string? Error { get; private set; }

    /// <summary>
    /// Gets the number of failed processing attempts.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Gets the next eligible processing time after a failure.
    /// </summary>
    public DateTimeOffset? NextAttempt { get; private set; }

    /// <summary>
    /// Initializes a new pending outbox message.
    /// </summary>
    public OutboxMessage(Guid id, string type, string content, DateTimeOffset occurredOn)
    {
        Id = id;
        Type = type;
        Content = content;
        OccurredOn = occurredOn;
        Status = OutboxMessageStatus.Pending;
    }

    /// <summary>
    /// Marks the message as successfully processed.
    /// </summary>
    public void MarkAsProcessed(DateTimeOffset processedOn)
    {
        Status = OutboxMessageStatus.Processed;
        ProcessedOn = processedOn;
        Error = null;
        NextAttempt = null;
    }

    /// <summary>
    /// Records a retriable failure and schedules the next attempt.
    /// </summary>
    public void MarkAsFailed(string error, DateTimeOffset nextAttempt)
    {
        Status = OutboxMessageStatus.Pending;
        Error = error;
        RetryCount++;
        NextAttempt = nextAttempt;
    }

    /// <summary>
    /// Moves the message to the dead-letter state after exhausting retries.
    /// </summary>
    public void MarkAsDeadLetter(string error)
    {
        Status = OutboxMessageStatus.DeadLetter;
        Error = error;
        RetryCount++;
        NextAttempt = null;
    }

    /// <summary>
    /// Requeues a dead-lettered message for replay.
    /// </summary>
    public void RequeueForReplay()
    {
        Status = OutboxMessageStatus.Pending;
        ProcessedOn = null;
        Error = null;
        RetryCount = 0;
        NextAttempt = null;
    }

    private OutboxMessage()
    {
    }
}
