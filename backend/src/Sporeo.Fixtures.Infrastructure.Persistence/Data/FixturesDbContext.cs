using Microsoft.EntityFrameworkCore;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.BuildingBlocks.Application.Abstractions.Messaging;
using Sporeo.BuildingBlocks.Domain.Events;
using Sporeo.BuildingBlocks.Domain.Models;
using Sporeo.BuildingBlocks.Infrastructure.Outbox;
using Sporeo.Fixtures.Domain.Fixtures;
using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Seasons;
using Sporeo.Fixtures.Domain.Sports;
using Sporeo.Fixtures.Domain.Venues;
using System.Linq.Expressions;
using System.Text.Json;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Data;

/// <summary>
/// EF Core database context for the Fixtures bounded context.
/// Implements <see cref="IUnitOfWork"/> and writes pending domain events to the durable outbox
/// in the same SaveChanges transaction as aggregate changes.
/// </summary>
public class FixturesDbContext : DbContext, IUnitOfWork
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    private readonly IDomainEventTypeRegistry _eventTypeRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixturesDbContext"/> class.
    /// </summary>
    public FixturesDbContext(
        DbContextOptions<FixturesDbContext> options,
        IDomainEventTypeRegistry eventTypeRegistry)
        : base(options)
    {
        _eventTypeRegistry = eventTypeRegistry;
    }

    /// <summary>
    /// Gets the set of fixtures.
    /// </summary>
    public DbSet<Fixture> Fixtures { get; set; } = null!;

    /// <summary>
    /// Gets the set of venues.
    /// </summary>
    public DbSet<Venue> Venues { get; set; } = null!;

    /// <summary>
    /// Gets the set of leagues.
    /// </summary>
    public DbSet<League> Leagues { get; set; } = null!;

    /// <summary>
    /// Gets the set of seasons.
    /// </summary>
    public DbSet<Season> Seasons { get; set; } = null!;

    /// <summary>
    /// Gets the set of sports.
    /// </summary>
    public DbSet<Sport> Sports { get; set; } = null!;

    /// <summary>
    /// Gets the set of outbox messages.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        CollectDomainEventsIntoOutbox();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FixturesDbContext).Assembly);

        IgnoreDomainEvents(modelBuilder);
        ApplySoftDeleteQueryFilters(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private void CollectDomainEventsIntoOutbox()
    {
        var outboxMessages = ChangeTracker
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
                type: _eventTypeRegistry.GetTypeKey(domainEvent),
                content: JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions),
                occurredOn: domainEvent.OccurredOn))
            .ToList();

        if (outboxMessages.Count != 0)
            OutboxMessages.AddRange(outboxMessages);
    }

    private static void IgnoreDomainEvents(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IHasDomainEvents).IsAssignableFrom(entityType.ClrType))
                modelBuilder.Entity(entityType.ClrType).Ignore(nameof(IHasDomainEvents.DomainEvents));
        }
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IDeletable).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(IDeletable.IsDeleted));
            var condition = Expression.Equal(property, Expression.Constant(false));
            var lambda = Expression.Lambda(condition, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
