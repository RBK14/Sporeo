using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sporeo.BuildingBlocks.Domain.Time;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Abstractions;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Configuration;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Models;

namespace Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Persistence;

/// <summary>
/// Generic EF Core outbox store backed by a module-specific database context.
/// </summary>
/// <typeparam name="TDbContext">The database context containing the outbox set.</typeparam>
public sealed class EfOutboxStore<TDbContext>(
    TDbContext dbContext,
    IOptions<OutboxOptions> options) : IOutboxStore
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var now = SystemTimeProvider.Now;
        var leaseExpiresOn = now.Add(options.Value.ClaimLease);
        var maxRetries = options.Value.MaxRetries;

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

        var claimed = new List<OutboxMessage>(messages.Count);

        foreach (var message in messages)
        {
            if (message.Status == OutboxMessageStatus.Processing)
            {
                if (message.RetryCount + 1 >= maxRetries)
                {
                    message.MarkAsDeadLetter("Processing lease expired; retry budget exhausted.");
                    continue;
                }

                message.MarkAsReclaimed(leaseExpiresOn);
            }
            else
            {
                message.MarkAsProcessing(leaseExpiresOn);
            }

            claimed.Add(message);
        }

        if (messages.Count > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return claimed;
    }

    /// <inheritdoc />
    public Task CommitChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
