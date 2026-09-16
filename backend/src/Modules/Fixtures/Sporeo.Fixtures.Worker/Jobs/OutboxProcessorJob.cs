using MediatR;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Sporeo.BuildingBlocks.Application.Abstractions.Messaging;
using Sporeo.BuildingBlocks.Application.Events;
using Sporeo.BuildingBlocks.Domain.Events;
using Sporeo.BuildingBlocks.Domain.Time;
using Sporeo.BuildingBlocks.Infrastructure.Outbox;
using Sporeo.Fixtures.Worker.Observability;
using System.Text.Json;
using Sporeo.Fixtures.Infrastructure.Persistence.Data;

namespace Sporeo.Fixtures.Worker.Jobs;

/// <summary>
/// Polls durable outbox messages and publishes corresponding domain-event notifications.
/// </summary>
/// <remarks>
/// Creates a fresh DI scope per firing. Processing is at-least-once; handlers must be idempotent.
/// Messages are marked processed only after a successful notification publish.
/// </remarks>
[DisallowConcurrentExecution]
internal sealed class OutboxProcessorJob(
    IServiceScopeFactory scopeFactory,
    IDomainEventTypeRegistry eventTypeRegistry,
    ILogger<OutboxProcessorJob> logger) : IJob
{
    private const int BatchSize = 20;
    private const int MaxRetries = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixturesDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var now = SystemTimeProvider.Now;

        var messages = await dbContext.OutboxMessages
            .Where(message =>
                message.Status == OutboxMessageStatus.Pending &&
                (message.NextAttempt == null || message.NextAttempt <= now))
            .OrderBy(message => message.OccurredOn)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return;

        logger.LogInformation("Processing {Count} outbox events.", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                if (!eventTypeRegistry.TryResolve(message.Type, out var eventType))
                    throw new InvalidOperationException($"Event type '{message.Type}' is not registered.");

                var domainEvent = JsonSerializer.Deserialize(message.Content, eventType, JsonOptions) as IDomainEvent
                    ?? throw new InvalidOperationException($"Event {message.Id} is null after deserialization.");

                var wrapperType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
                var notification = Activator.CreateInstance(wrapperType, domainEvent)
                    ?? throw new InvalidOperationException($"Unable to create notification for {message.Type}.");

                await publisher.Publish(notification, cancellationToken);
                message.MarkAsProcessed(SystemTimeProvider.Now);
                FixturesWorkerMetrics.OutboxProcessed.Add(1);
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
                    FixturesWorkerMetrics.OutboxDeadLettered.Add(1);
                }
                else
                {
                    var backoffMinutes = Math.Pow(2, message.RetryCount);
                    message.MarkAsFailed(ex.Message, now.AddMinutes(backoffMinutes));
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
