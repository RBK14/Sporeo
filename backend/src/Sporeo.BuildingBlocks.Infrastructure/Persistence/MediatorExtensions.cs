using MediatR;
using Microsoft.EntityFrameworkCore;
using Sporeo.BuildingBlocks.Domain.Events;

namespace Sporeo.BuildingBlocks.Infrastructure.Persistence;

public static class MediatorExtensions
{
    public static async Task DispatchDomainEventsAsync(this IPublisher publisher, DbContext context)
    {
        var domainEntities = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Count != 0)
            .ToList();

        if (domainEntities.Count == 0)
            return;

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        domainEntities.ForEach(entry => entry.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
            await publisher.Publish(domainEvent);
    }
}
