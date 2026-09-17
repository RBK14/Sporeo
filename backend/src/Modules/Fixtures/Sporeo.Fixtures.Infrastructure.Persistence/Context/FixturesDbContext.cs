using Microsoft.EntityFrameworkCore;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Abstractions;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Models;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Persistence;
using Sporeo.BuildingBlocks.Infrastructure.Persistence.QueryFilters;
using Sporeo.Fixtures.Domain.Fixtures;
using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Seasons;
using Sporeo.Fixtures.Domain.Sports;
using Sporeo.Fixtures.Domain.Venues;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Context;

/// <summary>
/// EF Core database context for the Fixtures bounded context.
/// Implements <see cref="IUnitOfWork"/> and writes pending domain events to the durable outbox
/// in the same SaveChanges transaction as aggregate changes.
/// </summary>
public class FixturesDbContext : DbContext, IUnitOfWork
{
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
        this.CollectDomainEventsIntoOutbox(_eventTypeRegistry);
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FixturesDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());

        modelBuilder.IgnoreDomainEvents();
        modelBuilder.ApplySoftDeleteQueryFilters();

        base.OnModelCreating(modelBuilder);
    }

}
