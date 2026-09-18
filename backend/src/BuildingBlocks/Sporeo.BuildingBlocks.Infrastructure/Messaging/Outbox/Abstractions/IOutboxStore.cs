using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Models;

namespace Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Abstractions;

/// <summary>
/// Persistence port for durable outbox messages.
/// </summary>
public interface IOutboxStore
{
    /// <summary>
    /// Loads the next batch of pending outbox messages eligible for processing.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits pending store changes after message status updates.
    /// </summary>
    Task CommitChangesAsync(CancellationToken cancellationToken = default);
}
