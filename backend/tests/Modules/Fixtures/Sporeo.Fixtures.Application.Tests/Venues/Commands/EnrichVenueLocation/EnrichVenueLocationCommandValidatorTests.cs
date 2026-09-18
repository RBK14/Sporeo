using FluentAssertions;
using Sporeo.Fixtures.Domain.Venues.Events;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;
using VenueAggregate = Sporeo.Fixtures.Domain.Venues.Venue;

namespace Sporeo.Fixtures.Application.Tests.Venues.Commands;

public sealed class VenueCreatedDomainEventEmissionTests
{
    [Fact]
    public void CreateFromProvider_WithoutCoordinates_ShouldRaiseVenueCreated()
    {
        var venue = VenueAggregate.CreateFromProvider(
            "Stadium",
            "TheSportsDB",
            "1",
            Address.Create(null, "Warsaw", "Poland").Value).Value;

        venue.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is VenueCreatedDomainEvent);
    }

    [Fact]
    public void CreateFromProvider_WithCoordinates_ShouldNotRaiseVenueCreated()
    {
        var venue = VenueAggregate.CreateFromProvider(
            "Stadium",
            "TheSportsDB",
            "1",
            Address.Create(null, "Warsaw", "Poland").Value,
            Coordinates.Create(52.2, 21.0).Value).Value;

        venue.DomainEvents.Should().BeEmpty();
    }
}
