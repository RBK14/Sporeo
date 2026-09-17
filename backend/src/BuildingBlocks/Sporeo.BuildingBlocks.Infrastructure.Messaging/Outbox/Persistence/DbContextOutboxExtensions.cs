using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sporeo.BuildingBlocks.Domain.Events;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Abstractions;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Models;

namespace Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Persistence;

/// <summary>
/// Shared EF Core helpers for collecting domain events into the durable outbox.
/// </summary>
public static class DbContextOutboxExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    /// <summary>
    /// Serializes pending domain events from tracked aggregates into outbox messages.
    /// </summary>
    public static void CollectDomainEventsIntoOutbox(
        this DbContext dbContext,
        IDomainEventTypeRegistry eventTypeRegistry)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(eventTypeRegistry);

        var outboxMessages = dbContext.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                var domainEvents = entity.DomainEvents.ToList();
                entity.ClearDomainEvents();
                return domainEvents;
            })
            .Select(domainEvent => new OutboxMessage(
                id: domainEvent.EventId,
                type: eventTypeRegistry.GetTypeKey(domainEvent),
                content: JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions),
                occurredOn: domainEvent.OccurredOn))
            .ToList();

        if (outboxMessages.Count != 0)
            dbContext.Set<OutboxMessage>().AddRange(outboxMessages);
    }

    /// <summary>
    /// Ignores the in-memory domain-event collection for all entities implementing <see cref="IHasDomainEvents"/>.
    /// </summary>
    public static void IgnoreDomainEvents(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IHasDomainEvents).IsAssignableFrom(entityType.ClrType))
                modelBuilder.Entity(entityType.ClrType).Ignore(nameof(IHasDomainEvents.DomainEvents));
        }
    }
}
