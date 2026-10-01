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
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var now = SystemTimeProvider.Now;
        var leaseExpiresOn = now.Add(ClaimLease);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // UPDLOCK + READPAST claims rows without blocking other workers on already-locked rows.
        var messages = await dbContext.Set<OutboxMessage>()
            .FromSqlInterpolated($"""
                SELECT TOP ({batchSize}) *
                FROM [outbox-messages] WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE ([Status] = {nameof(OutboxMessageStatus.Pending)}
                      AND ([NextAttempt] IS NULL OR [NextAttempt] <= {now}))
                   OR ([Status] = {nameof(OutboxMessageStatus.Processing)}
                      AND [NextAttempt] IS NOT NULL
                      AND [NextAttempt] <= {now})
                ORDER BY [OccurredOn]
                """)
            .AsTracking()
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
            message.MarkAsProcessing(leaseExpiresOn);

        if (messages.Count > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return messages;
    }

    /// <inheritdoc />
    public Task CommitChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
