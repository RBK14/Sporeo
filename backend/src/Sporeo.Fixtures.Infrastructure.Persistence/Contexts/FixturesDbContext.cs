using MediatR;
using Microsoft.EntityFrameworkCore;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.BuildingBlocks.Domain.Events;
using Sporeo.BuildingBlocks.Domain.Models;
using Sporeo.BuildingBlocks.Infrastructure.Persistence;
using Sporeo.Fixtures.Domain.Fixtures;
using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Seasons;
using Sporeo.Fixtures.Domain.Sports;
using Sporeo.Fixtures.Domain.Venues;
using System.Linq.Expressions;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Contexts;

/// <summary>
/// EF Core database context for the Fixtures bounded context.
/// Implements <see cref="IUnitOfWork"/> and dispatches domain events before persisting changes.
/// </summary>
/// <param name="options">The EF Core options for this context.</param>
/// <param name="publisher">The MediatR publisher used to dispatch domain events.</param>
public class FixturesDbContext(
    DbContextOptions<FixturesDbContext> options,
    IPublisher publisher) : DbContext(options), IUnitOfWork
{
    private readonly IPublisher _publisher = publisher;

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

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _publisher.DispatchDomainEventsAsync(this);
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

    private static void IgnoreDomainEvents(ModelBuilder modelBuilder)
    {
        var entityTypes = modelBuilder.Model.GetEntityTypes();
        foreach (var entityType in entityTypes)
        {
            if (typeof(IHasDomainEvents).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Ignore(nameof(IHasDomainEvents.DomainEvents));
            }
        }
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        var entityTypes = modelBuilder.Model.GetEntityTypes();
        foreach (var entityType in entityTypes)
        {
            if (typeof(IDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(IDeletable.IsDeleted));
                var condition = Expression.Equal(property, Expression.Constant(false));
                var lambda = Expression.Lambda(condition, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}
