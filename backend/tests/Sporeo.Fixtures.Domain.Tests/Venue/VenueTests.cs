using FluentAssertions;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;
using VenueAggregate = Sporeo.Fixtures.Domain.Venues.Venue;

namespace Sporeo.Fixtures.Domain.Tests.Venue;

public class VenueTests
{
    private static Address ValidAddress =>
        Address.Create("Main St 1", "Warsaw", "Poland").Value;

    private static Coordinates ValidCoordinates =>
        Coordinates.Create(52.2297, 21.0122).Value;

    [Fact]
    public void CreateManually_ShouldCreateVenue()
    {
        var result = VenueAggregate.CreateManually("Stadium Arena", ValidAddress, ValidCoordinates);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Stadium Arena");
        result.Value.Address.Should().Be(ValidAddress);
        result.Value.Coordinates.Should().Be(ValidCoordinates);
        result.Value.IsManuallyEdited.Should().BeTrue();
        result.Value.ExternalProviderName.Should().BeNull();
        result.Value.ExternalProviderId.Should().BeNull();
    }

    [Fact]
    public void CreateManually_WithEmptyName_ShouldFail()
    {
        var result = VenueAggregate.CreateManually("  ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Venue.EmptyName);
    }

    [Fact]
    public void CreateFromProvider_ShouldPersistProviderMetadata()
    {
        var result = VenueAggregate.CreateFromProvider(
            "Stadium Arena",
            "ProviderA",
            "external-123",
            ValidAddress,
            ValidCoordinates);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsManuallyEdited.Should().BeFalse();
        result.Value.ExternalProviderName.Should().Be("ProviderA");
        result.Value.ExternalProviderId.Should().Be("external-123");
    }

    [Theory]
    [InlineData("", "external-123")]
    [InlineData("ProviderA", "")]
    public void CreateFromProvider_WithMissingProviderData_ShouldFail(string providerName, string providerId)
    {
        var result = VenueAggregate.CreateFromProvider("Stadium Arena", providerName, providerId);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void SyncExternalData_FromProviderVenue_ShouldUpdateCoreFields()
    {
        var venue = CreateProviderVenue();
        var updatedAddress = Address.Create("New St 2", "Krakow", "Poland").Value;
        var updatedCoordinates = Coordinates.Create(50.0647, 19.9450).Value;

        var result = venue.SyncExternalData("Updated Arena", updatedAddress, updatedCoordinates);

        result.IsSuccess.Should().BeTrue();
        venue.Name.Should().Be("Updated Arena");
        venue.Address.Should().Be(updatedAddress);
        venue.Coordinates.Should().Be(updatedCoordinates);
    }

    [Fact]
    public void SyncExternalData_WhenManuallyEdited_ShouldFail()
    {
        var venue = CreateManualVenue();

        var result = venue.SyncExternalData("Provider Update");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Venue.LockedForSync);
    }

    [Fact]
    public void UpdateManually_ShouldUpdateCoreFieldsAndLockSync()
    {
        var venue = CreateProviderVenue();
        var updatedAddress = Address.Create("Manual St 3", "Gdansk", "Poland").Value;
        var updatedCoordinates = Coordinates.Create(54.3520, 18.6466).Value;

        var result = venue.UpdateManually("Manual Arena", updatedAddress, updatedCoordinates);

        result.IsSuccess.Should().BeTrue();
        venue.Name.Should().Be("Manual Arena");
        venue.Address.Should().Be(updatedAddress);
        venue.Coordinates.Should().Be(updatedCoordinates);
        venue.IsManuallyEdited.Should().BeTrue();
        venue.SyncExternalData("Provider Update").Error.Should().Be(Errors.Venue.LockedForSync);
    }

    [Fact]
    public void Delete_ShouldBeIdempotent()
    {
        var venue = CreateManualVenue();

        venue.Delete().IsSuccess.Should().BeTrue();
        var secondDelete = venue.Delete();

        secondDelete.IsSuccess.Should().BeTrue();
        venue.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void DeletedVenue_ShouldBlockBusinessMutations()
    {
        var venue = CreateManualVenue();
        venue.Delete().IsSuccess.Should().BeTrue();

        venue.SyncExternalData("Updated").Error.Should().Be(Errors.Venue.Deleted);
        venue.UpdateManually("Updated").Error.Should().Be(Errors.Venue.Deleted);
    }

    private static VenueAggregate CreateManualVenue()
    {
        return VenueAggregate.CreateManually("Stadium Arena", ValidAddress, ValidCoordinates).Value;
    }

    private static VenueAggregate CreateProviderVenue()
    {
        return VenueAggregate.CreateFromProvider(
            "Stadium Arena",
            "ProviderA",
            "external-123",
            ValidAddress,
            ValidCoordinates).Value;
    }
}
