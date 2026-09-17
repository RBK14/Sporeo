using Microsoft.EntityFrameworkCore;
using Sporeo.BuildingBlocks.Domain.Time;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Abstractions;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Models;

namespace Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Persistence;

/// <summary>
/// Generic EF Core outbox store backed by a module-specific database context.
/// </summary>
/// <typeparam name="TDbContext">The database context containing the outbox set.</typeparam>
public sealed class EfOutboxStore<TDbContext>(TDbContext dbContext) : IOutboxStore
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var now = SystemTimeProvider.Now;

        return await dbContext.Set<OutboxMessage>()
            .Where(message =>
                message.Status == OutboxMessageStatus.Pending &&
                (message.NextAttempt == null || message.NextAttempt <= now))
            .OrderBy(message => message.OccurredOn)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task CommitChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
