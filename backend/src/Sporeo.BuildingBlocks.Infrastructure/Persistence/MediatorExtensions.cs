using MediatR;
using Microsoft.EntityFrameworkCore;
using Sporeo.BuildingBlocks.Domain.Events;

namespace Sporeo.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Extension methods for dispatching domain events collected by EF Core change tracking.
/// </summary>
public static class MediatorExtensions
{
    /// <summary>
    /// Publishes all pending domain events from tracked <see cref="IHasDomainEvents"/> entities
    /// via MediatR and clears them from the aggregates after successful publication.
    /// </summary>
    /// <param name="publisher">The MediatR publisher used to dispatch notifications.</param>
    /// <param name="context">The EF Core context whose change tracker is inspected.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// Call this immediately before <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>
    /// so that handlers can participate in the same unit of work. Events are cleared only after
    /// every notification has been published successfully.
    /// </remarks>
    public static async Task DispatchDomainEventsAsync(this IPublisher publisher, DbContext context, CancellationToken cancellationToken = default)
    {
        var domainEntities = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(x => x.Entity.DomainEvents is { Count: > 0 })
            .ToList();

        if (domainEntities.Count == 0)
            return;

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        foreach (var domainEvent in domainEvents)
            await publisher.Publish(domainEvent, cancellationToken);

        domainEntities.ForEach(entry => entry.Entity.ClearDomainEvents());
    }
}
