using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Domain.Events;
using Sporeo.BuildingBlocks.Domain.Time;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Abstractions;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Models;

namespace Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Processing;

/// <summary>
/// Processes pending outbox messages with retry and dead-letter handling.
/// </summary>
public sealed class OutboxProcessor(
    IOutboxStore outboxStore,
    IDomainEventTypeRegistry eventTypeRegistry,
    IOutboxMessageDispatcher dispatcher,
    ILogger<OutboxProcessor> logger) : IOutboxProcessor
{
    private const int DefaultBatchSize = 20;
    private const int MaxRetries = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public async Task<OutboxProcessingResult> ProcessOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        var now = SystemTimeProvider.Now;
        var messages = await outboxStore.GetUnprocessedMessagesAsync(DefaultBatchSize, cancellationToken);

        if (messages.Count == 0)
            return new OutboxProcessingResult(0, 0, 0);

        logger.LogInformation("Processing {Count} outbox events.", messages.Count);

        var processed = 0;
        var deadLettered = 0;
        var retried = 0;

        foreach (var message in messages)
        {
            try
            {
                if (!eventTypeRegistry.TryResolve(message.Type, out var eventType))
                    throw new InvalidOperationException($"Event type '{message.Type}' is not registered.");

                var domainEvent = JsonSerializer.Deserialize(message.Content, eventType, JsonOptions) as IDomainEvent
                    ?? throw new InvalidOperationException($"Event {message.Id} is null after deserialization.");

                await dispatcher.DispatchAsync(domainEvent, eventType, cancellationToken);
                message.MarkAsProcessed(SystemTimeProvider.Now);
                processed++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing outbox event {EventId}.", message.Id);

                if (message.RetryCount + 1 >= MaxRetries)
                {
                    logger.LogCritical("Outbox event {EventId} moved to dead-letter.", message.Id);
                    message.MarkAsDeadLetter(ex.Message);
                    deadLettered++;
                }
                else
                {
                    var backoffMinutes = Math.Pow(2, message.RetryCount);
                    message.MarkAsFailed(ex.Message, now.AddMinutes(backoffMinutes));
                    retried++;
                }
            }
        }

        await outboxStore.CommitChangesAsync(cancellationToken);
        return new OutboxProcessingResult(processed, deadLettered, retried);
    }
}
