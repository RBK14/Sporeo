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

namespace Sporeo.Fixtures.Infrastructure.Persistence;

public class FixturesDbContext(DbContextOptions<FixturesDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Fixture> Fixtures { get; set; }
    public DbSet<Venue> Venues { get; set; }
    public DbSet<League> Leagues { get; set; }
    public DbSet<Season> Seasons { get; set; }
    public DbSet<Sport> Sports { get; set; }

    public async Task<bool> SaveEntitiesAsync(IPublisher publisher, CancellationToken cancellationToken = default)
    {
        await publisher.DispatchDomainEventsAsync(this);
        await base.SaveChangesAsync(cancellationToken);

        return true;

    }

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
