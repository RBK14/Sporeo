using MediatR;
using Sporeo.BuildingBlocks.Domain.Events;

namespace Sporeo.BuildingBlocks.Application.Messaging.DomainEvents;

/// <summary>
/// A wrapper that adapts a pure <see cref="IDomainEvent"/> into a MediatR <see cref="INotification"/>.
/// </summary>
/// <typeparam name="TDomainEvent">The type of the domain event.</typeparam>
public sealed class DomainEventNotification<TDomainEvent>(TDomainEvent domainEvent) : INotification
    where TDomainEvent : IDomainEvent
{
    /// <summary>
    /// Gets the underlying domain event.
    /// </summary>
    public TDomainEvent DomainEvent { get; } = domainEvent;
}
