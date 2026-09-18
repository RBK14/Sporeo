using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sporeo.BuildingBlocks.Domain.Models;

namespace Sporeo.BuildingBlocks.Infrastructure.Persistence.QueryFilters;

/// <summary>
/// Configures global soft-delete filters for entities implementing <see cref="IDeletable"/>.
/// </summary>
public static class SoftDeleteQueryFilterExtensions
{
    private const string SoftDeleteFilterName = "SoftDeletionFilter";

    /// <summary>
    /// Adds a global <c>IsDeleted == false</c> filter to every soft-deletable root entity.
    /// Existing query filters are preserved and combined with the soft-delete condition.
    /// </summary>
    public static ModelBuilder ApplySoftDeleteQueryFilters(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IDeletable).IsAssignableFrom(entityType.ClrType))
                continue;

            if (entityType.BaseType is not null)
            {
                if (!typeof(IDeletable).IsAssignableFrom(entityType.GetRootType().ClrType))
                {
                    throw new InvalidOperationException(
                        $"Soft-deletable entity '{entityType.ClrType.FullName}' must have a soft-deletable root entity.");
                }

                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var isDeleted = Expression.Property(parameter, nameof(IDeletable.IsDeleted));
            var filterBody = Expression.Equal(isDeleted, Expression.Constant(false));

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(SoftDeleteFilterName, Expression.Lambda(filterBody, parameter));
        }

        return modelBuilder;
    }
}
