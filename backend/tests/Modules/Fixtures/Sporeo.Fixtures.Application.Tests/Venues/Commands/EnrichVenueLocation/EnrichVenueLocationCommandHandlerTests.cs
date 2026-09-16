using FluentAssertions;
using NSubstitute;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Venues.Abstractions.Geocoding;
using Sporeo.Fixtures.Application.Venues.Abstractions.Repositories;
using Sporeo.Fixtures.Application.Venues.Commands.EnrichVenueLocation;
using Sporeo.Fixtures.Domain.Venues;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Application.Tests.Venues.Commands;

public sealed class EnrichVenueLocationCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenGeocodingFails_ShouldPropagateFailure()
    {
        var venue = Venue.CreateFromProvider(
            "National Stadium",
            "TheSportsDB",
            "1",
            Address.Create(null, "Warsaw", "Poland").Value).Value;

        var repository = Substitute.For<IVenueRepository>();
        repository.GetByIdAsync(Arg.Any<VenueId>(), Arg.Any<CancellationToken>())
            .Returns(venue);

        var geocoding = Substitute.For<IGeocodingService>();
        geocoding.GetVenueLocationAsync(
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Failure<GeocodedLocation>(
                new Error("Geocoding.Transient", "Nominatim unavailable")));

        var handler = new EnrichVenueLocationCommandHandler(repository, geocoding);

        var result = await handler.Handle(new EnrichVenueLocationCommand(venue.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Geocoding.Transient");
        venue.Coordinates.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenCoordinatesAlreadyPresent_ShouldSucceedWithoutGeocoding()
    {
        var coordinates = Coordinates.Create(52.2, 21.0).Value;
        var venue = Venue.CreateFromProvider(
            "National Stadium",
            "TheSportsDB",
            "1",
            Address.Create(null, "Warsaw", "Poland").Value,
            coordinates).Value;

        var repository = Substitute.For<IVenueRepository>();
        repository.GetByIdAsync(Arg.Any<VenueId>(), Arg.Any<CancellationToken>())
            .Returns(venue);

        var geocoding = Substitute.For<IGeocodingService>();
        var handler = new EnrichVenueLocationCommandHandler(repository, geocoding);

        var result = await handler.Handle(new EnrichVenueLocationCommand(venue.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await geocoding.DidNotReceiveWithAnyArgs()
            .GetVenueLocationAsync(default!, default, default, default, default);
    }
}
