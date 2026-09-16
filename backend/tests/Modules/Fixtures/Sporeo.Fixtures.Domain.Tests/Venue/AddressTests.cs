using FluentAssertions;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Domain.Tests.Venue;

public class AddressTests
{
    [Fact]
    public void Create_WithValidValues_ShouldSucceed()
    {
        var result = Address.Create("Main St 1", "Warsaw", "Poland");

        result.IsSuccess.Should().BeTrue();
        result.Value.Street.Should().Be("Main St 1");
        result.Value.City.Should().Be("Warsaw");
        result.Value.Country.Should().Be("Poland");
    }

    [Fact]
    public void Create_WithNullStreet_ShouldSucceed()
    {
        var result = Address.Create(null, "Warsaw", "Poland");

        result.IsSuccess.Should().BeTrue();
        result.Value.Street.Should().BeNull();
        result.Value.City.Should().Be("Warsaw");
        result.Value.Country.Should().Be("Poland");
    }

    [Theory]
    [InlineData(" ", "Warsaw", "Poland", "Venue.Address.EmptyStreet")]
    [InlineData(null, " ", "Poland", "Venue.Address.EmptyCity")]
    [InlineData(null, "Warsaw", " ", "Venue.Address.EmptyCountry")]
    [InlineData(null, null, "Poland", "Venue.Address.EmptyCity")]
    [InlineData(null, "Warsaw", null, "Venue.Address.EmptyCountry")]
    public void Create_WithInvalidValues_ShouldFail(
        string? street,
        string? city,
        string? country,
        string expectedErrorCode)
    {
        var result = Address.Create(street, city!, country!);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expectedErrorCode);
    }

    [Fact]
    public void Equals_WithSameValues_ShouldBeEqual()
    {
        var left = Address.Create("Main St 1", "Warsaw", "Poland").Value;
        var right = Address.Create("Main St 1", "Warsaw", "Poland").Value;

        left.Should().Be(right);
    }
}
