using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NetTopologySuite.Geometries;
using Sporeo.Fixtures.Domain.Venues;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Extensions;

/// <summary>
/// Synchronizes the <c>Location</c> geography shadow property from venue coordinates
/// so spatial queries can use the SQL Server spatial index.
/// </summary>
public sealed class VenueLocationInterceptor : SaveChangesInterceptor
{
    private const string LocationPropertyName = "Location";
    private const int Wgs84Srid = 4326;

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        SyncLocations(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SyncLocations(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void SyncLocations(DbContext? context)
    {
        if (context is null)
            return;

        foreach (var entry in context.ChangeTracker.Entries<Venue>())
        {
            if (entry.State is EntityState.Deleted or EntityState.Detached)
                continue;

            var coordinatesTarget = entry.Reference(nameof(Venue.Coordinates)).TargetEntry;
            var shouldSync = entry.State is EntityState.Added or EntityState.Modified
                || coordinatesTarget is { State: not EntityState.Unchanged };

            if (!shouldSync)
                continue;

            var locationProperty = entry.Property(LocationPropertyName);
            var coordinates = entry.Entity.Coordinates;

            if (coordinates is null || coordinatesTarget?.State == EntityState.Deleted)
            {
                locationProperty.CurrentValue = null;
                locationProperty.IsModified = true;
                continue;
            }

            // NetTopologySuite uses X = longitude, Y = latitude (opposite of SQL Server geography::Point).
            locationProperty.CurrentValue = new Point(coordinates.Longitude, coordinates.Latitude)
            {
                SRID = Wgs84Srid
            };
            locationProperty.IsModified = true;
        }
    }
}
