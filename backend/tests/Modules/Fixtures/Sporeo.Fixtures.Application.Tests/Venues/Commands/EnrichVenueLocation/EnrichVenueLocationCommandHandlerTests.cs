using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Venues.Abstractions;
using Sporeo.Fixtures.Application.Venues.Data;
using Sporeo.Fixtures.Application.Venues.Commands.EnrichVenueLocation;
using Sporeo.Fixtures.Domain.Venues;
using Sporeo.Fixtures.Domain.Venues.Enums;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;
using Errors = Sporeo.Fixtures.Application.Common.Errors;

namespace Sporeo.Fixtures.Application.Tests.Venues.Commands;

public sealed class EnrichVenueLocationCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenGeocodingFailsTechnically_ShouldRecordFailedStatusAndSucceed()
    {
        var venue = CreateVenueWithoutCoordinates();
        var handler = CreateHandler(venue, Result.Failure<GeocodedLocation>(Errors.Geocoding.HttpError));

        var result = await handler.Handle(new EnrichVenueLocationCommand(venue.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        venue.Coordinates.Should().BeNull();
        venue.GeocodingStatus.Should().Be(GeocodingStatus.Failed);
        venue.GeocodingErrorCode.Should().Be(Errors.Geocoding.HttpError.Code);
        venue.LastGeocodingAttemptOn.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenLocationNotFound_ShouldRecordNotFoundStatus()
    {
        var venue = CreateVenueWithoutCoordinates();
        var handler = CreateHandler(venue, Result.Failure<GeocodedLocation>(Errors.Geocoding.NotFound));

        var result = await handler.Handle(new EnrichVenueLocationCommand(venue.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        venue.GeocodingStatus.Should().Be(GeocodingStatus.NotFound);
        venue.GeocodingErrorCode.Should().Be(Errors.Geocoding.NotFound.Code);
    }

    [Fact]
    public async Task Handle_WhenGeocodingSucceeds_ShouldResolveLocation()
    {
        var venue = CreateVenueWithoutCoordinates();
        var coordinates = Coordinates.Create(52.2, 21.0).Value;
        var handler = CreateHandler(venue, Result.Success(new GeocodedLocation(coordinates, null)));

        var result = await handler.Handle(new EnrichVenueLocationCommand(venue.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        venue.Coordinates.Should().Be(coordinates);
        venue.GeocodingStatus.Should().Be(GeocodingStatus.Resolved);
        venue.LastGeocodingAttemptOn.Should().NotBeNull();
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
        var handler = new EnrichVenueLocationCommandHandler(
            repository,
            geocoding,
            NullLogger<EnrichVenueLocationCommandHandler>.Instance);

        var result = await handler.Handle(new EnrichVenueLocationCommand(venue.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await geocoding.DidNotReceiveWithAnyArgs()
            .GetVenueLocationAsync(default!, default, default, default, default);
    }

    private static Venue CreateVenueWithoutCoordinates() =>
        Venue.CreateFromProvider(
            "National Stadium",
            "TheSportsDB",
            "1",
            Address.Create(null, "Warsaw", "Poland").Value).Value;

    private static EnrichVenueLocationCommandHandler CreateHandler(Venue venue, Result<GeocodedLocation> geocodingResult)
    {
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
            .Returns(geocodingResult);

        return new EnrichVenueLocationCommandHandler(
            repository,
            geocoding,
            NullLogger<EnrichVenueLocationCommandHandler>.Instance);
    }
}
