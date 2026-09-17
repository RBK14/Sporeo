using FluentAssertions;
using Sporeo.Fixtures.Application.Venues.Commands.EnrichVenueLocation;
using Sporeo.Fixtures.Domain.Venues.Events;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;
using VenueAggregate = Sporeo.Fixtures.Domain.Venues.Venue;

namespace Sporeo.Fixtures.Application.Tests.Venues.Commands;

public sealed class EnrichVenueLocationCommandValidatorTests
{
    private readonly EnrichVenueLocationCommandValidator _validator = new();

    [Fact]
    public void Validate_WithEmptyVenueId_ShouldFail()
    {
        var result = _validator.Validate(new EnrichVenueLocationCommand(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorCode == "Venue.InvalidId");
    }

    [Fact]
    public void Validate_WithVenueId_ShouldSucceed()
    {
        var result = _validator.Validate(new EnrichVenueLocationCommand(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}

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
