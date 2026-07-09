using FluentAssertions;
using Sporeo.Fixtures.Domain;
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
    public void Create_WithAllNullValues_ShouldSucceed()
    {
        var result = Address.Create(null, null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Street.Should().BeNull();
        result.Value.City.Should().BeNull();
        result.Value.Country.Should().BeNull();
    }

    [Theory]
    [InlineData(" ", null, null, "Venue.Address.EmptyStreet")]
    [InlineData(null, " ", null, "Venue.Address.EmptyCity")]
    [InlineData(null, null, " ", "Venue.Address.EmptyCountry")]
    public void Create_WithWhitespaceValue_ShouldFail(
        string? street,
        string? city,
        string? country,
        string expectedErrorCode)
    {
        var result = Address.Create(street, city, country);

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
