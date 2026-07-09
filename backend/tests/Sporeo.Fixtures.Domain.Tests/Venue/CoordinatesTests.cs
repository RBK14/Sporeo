using FluentAssertions;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Domain.Tests.Venue;

public class CoordinatesTests
{
    [Fact]
    public void Create_WithValidValues_ShouldSucceed()
    {
        var result = Coordinates.Create(52.2297, 21.0122);

        result.IsSuccess.Should().BeTrue();
        result.Value.Latitude.Should().Be(52.2297);
        result.Value.Longitude.Should().Be(21.0122);
    }

    [Theory]
    [InlineData(-90.1, 0)]
    [InlineData(90.1, 0)]
    public void Create_WithInvalidLatitude_ShouldFail(double latitude, double longitude)
    {
        var result = Coordinates.Create(latitude, longitude);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Venue.Coordinates.InvalidLatitude);
    }

    [Theory]
    [InlineData(0, -180.1)]
    [InlineData(0, 180.1)]
    public void Create_WithInvalidLongitude_ShouldFail(double latitude, double longitude)
    {
        var result = Coordinates.Create(latitude, longitude);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Venue.Coordinates.InvalidLongitude);
    }

    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    public void Create_WithBoundaryValues_ShouldSucceed(double latitude, double longitude)
    {
        var result = Coordinates.Create(latitude, longitude);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithSameValues_ShouldBeEqual()
    {
        var left = Coordinates.Create(52.2297, 21.0122).Value;
        var right = Coordinates.Create(52.2297, 21.0122).Value;

        left.Should().Be(right);
    }
}
