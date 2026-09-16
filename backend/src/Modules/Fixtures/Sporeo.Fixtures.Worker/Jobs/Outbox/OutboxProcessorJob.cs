using Quartz;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Abstractions;
using Sporeo.Fixtures.Worker.Observability;

namespace Sporeo.Fixtures.Worker.Jobs.Outbox;

/// <summary>
/// Quartz adapter that processes durable outbox messages through the shared processor.
/// </summary>
/// <remarks>
/// Creates a fresh DI scope per firing. Processing is at-least-once; handlers must be idempotent.
/// </remarks>
[DisallowConcurrentExecution]
internal sealed class OutboxProcessorJob(IServiceScopeFactory scopeFactory) : IJob
{
    /// <inheritdoc />
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

        var result = await processor.ProcessOutboxMessagesAsync(cancellationToken);

        if (result.Processed > 0)
            FixturesWorkerMetrics.OutboxProcessed.Add(result.Processed);

        if (result.DeadLettered > 0)
            FixturesWorkerMetrics.OutboxDeadLettered.Add(result.DeadLettered);
    }
}
