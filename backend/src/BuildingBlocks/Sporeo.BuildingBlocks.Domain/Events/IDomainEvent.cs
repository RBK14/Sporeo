namespace Sporeo.BuildingBlocks.Domain.Events;

/// <summary>
/// Represents a domain event that records something meaningful that happened in the domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the event occurrence.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    DateTimeOffset OccurredOn { get; }
}
