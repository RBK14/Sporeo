namespace Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Abstractions;

/// <summary>
/// Processes pending durable outbox messages.
/// </summary>
public interface IOutboxProcessor
{
    /// <summary>
    /// Processes a batch of pending outbox messages.
    /// </summary>
    Task<OutboxProcessingResult> ProcessOutboxMessagesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Summary of a single outbox processing run.
/// </summary>
/// <param name="Processed">Number of successfully processed messages.</param>
/// <param name="DeadLettered">Number of messages moved to dead-letter.</param>
/// <param name="Retried">Number of messages scheduled for retry.</param>
public sealed record OutboxProcessingResult(int Processed, int DeadLettered, int Retried);
