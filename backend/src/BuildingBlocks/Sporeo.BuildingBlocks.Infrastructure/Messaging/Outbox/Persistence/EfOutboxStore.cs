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
    private readonly string _tableName = QuoteIdentifier(
        dbContext.Model.FindEntityType(typeof(OutboxMessage))?.GetSchemaQualifiedTableName()
        ?? throw new InvalidOperationException(
            $"Entity type '{nameof(OutboxMessage)}' is not mapped on {typeof(TDbContext).Name}."));

    /// <inheritdoc />
    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            var now = SystemTimeProvider.Now;
            var leaseExpiresOn = now.Add(options.Value.ClaimLease);
            var maxRetries = options.Value.MaxRetries;

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            // UPDLOCK + READPAST claims rows without blocking other workers on already-locked rows.
            // Table name comes from EF model metadata (not user input); remaining values are parameterized.
            var pendingStatus = nameof(OutboxMessageStatus.Pending);
            var processingStatus = nameof(OutboxMessageStatus.Processing);

            var sql =
                "SELECT TOP ({0}) * " +
                $"FROM {_tableName} WITH (UPDLOCK, READPAST, ROWLOCK) " +
                "WHERE ([Status] = {1} AND ([NextAttempt] IS NULL OR [NextAttempt] <= {2})) " +
                "OR ([Status] = {3} AND [NextAttempt] IS NOT NULL AND [NextAttempt] <= {2}) " +
                "ORDER BY [OccurredOn]";

            var messages = await dbContext.Set<OutboxMessage>()
                .FromSqlRaw(sql, batchSize, pendingStatus, now, processingStatus)
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

            return (IReadOnlyList<OutboxMessage>)claimed;
        });
    }

    /// <inheritdoc />
    public Task CommitChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private static string QuoteIdentifier(string schemaQualifiedName)
    {
        // GetSchemaQualifiedTableName returns "schema.table" or "table".
        // Bracket each segment so hyphenated names like outbox-messages are valid T-SQL identifiers.
        var parts = schemaQualifiedName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join('.', parts.Select(part => $"[{part.Trim('[', ']')}]"));
    }
}
