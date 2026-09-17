using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sporeo.BuildingBlocks.Domain.Models;
using Sporeo.BuildingBlocks.Domain.Time;

namespace Sporeo.BuildingBlocks.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that populates audit and soft-delete timestamps for tracked entities.
/// </summary>
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditProperties(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditProperties(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateAuditProperties(DbContext? context)
    {
        if (context is null)
            return;

        var utcNow = SystemTimeProvider.Now;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditable)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property(nameof(IAuditable.CreatedOn)).CurrentValue = utcNow;
                    entry.Property(nameof(IAuditable.ModifiedOn)).CurrentValue = null;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property(nameof(IAuditable.ModifiedOn)).CurrentValue = utcNow;
                }
            }

            if (entry.Entity is IDeletable && entry.State is EntityState.Added or EntityState.Modified)
            {
                var isDeleted = entry.Property(nameof(IDeletable.IsDeleted));
                var deletedOn = entry.Property(nameof(IDeletable.DeletedOn));

                if (isDeleted.CurrentValue is true && deletedOn.CurrentValue is null)
                    deletedOn.CurrentValue = utcNow;
            }
        }
    }
}
