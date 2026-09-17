using Sporeo.BuildingBlocks.Domain.Events;

namespace Sporeo.Fixtures.Domain.Venues.Events;

/// <summary>
/// Raised when a venue aggregate is created and may require asynchronous enrichment.
/// </summary>
/// <param name="VenueId">The identifier of the created venue.</param>
public sealed record VenueCreatedDomainEvent(Guid VenueId) : DomainEvent;
