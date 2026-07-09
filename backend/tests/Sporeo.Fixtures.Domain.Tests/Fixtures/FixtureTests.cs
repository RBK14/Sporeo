using FluentAssertions;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Fixtures;
using Sporeo.Fixtures.Domain.Fixtures.Enums;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Domain.Tests.Fixtures;

public class FixtureTests
{
    private static readonly DateTimeOffset StartDate = new(2026, 2, 1, 18, 0, 0, TimeSpan.Zero);

    private static SportId SportId => SportId.FromValue(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static LeagueId LeagueId => LeagueId.FromValue(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static SeasonId? SeasonId => null;
    private static VenueId VenueId => VenueId.FromValue(Guid.Parse("33333333-3333-3333-3333-333333333333"));

    [Fact]
    public void CreateManually_ShouldCreateFixture()
    {
        var result = Fixture.CreateManually(SportId, LeagueId, SeasonId, "Home vs Away", StartDate);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Home vs Away");
        result.Value.Status.Should().Be(FixtureStatus.Scheduled);
        result.Value.IsManuallyEdited.Should().BeTrue();
    }

    [Fact]
    public void CreateManually_WithEmptyName_ShouldFail()
    {
        var result = Fixture.CreateManually(SportId, LeagueId, SeasonId, "  ", StartDate);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Fixture.EmptyName);
    }

    [Fact]
    public void CreateFromProvider_ShouldPersistProviderMetadata()
    {
        var result = Fixture.CreateFromProvider(
            SportId,
            LeagueId,
            SeasonId,
            "Home vs Away",
            StartDate,
            "ProviderA",
            "external-123");

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
        var result = Fixture.CreateFromProvider(
            SportId,
            LeagueId,
            SeasonId,
            "Home vs Away",
            StartDate,
            providerName,
            providerId);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void SyncExternalData_WhenManuallyEdited_ShouldFail()
    {
        var fixture = CreateManualFixture();

        var result = fixture.SyncExternalData(SportId, LeagueId, SeasonId, "Provider Update", StartDate);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Fixture.LockedForSync);
    }

    [Fact]
    public void UpdateManually_ShouldUpdateCoreFields()
    {
        var fixture = CreateProviderFixture();
        var updatedStartDate = StartDate.AddDays(1);

        var result = fixture.UpdateManually(SportId, LeagueId, "Updated Name", updatedStartDate);

        result.IsSuccess.Should().BeTrue();
        fixture.Name.Should().Be("Updated Name");
        fixture.StartDate.Should().Be(updatedStartDate);
        fixture.IsManuallyEdited.Should().BeTrue();
    }

    [Fact]
    public void AssignVenue_ShouldSetVenue()
    {
        var fixture = CreateManualFixture();

        var result = fixture.AssignVenue(VenueId);

        result.IsSuccess.Should().BeTrue();
        fixture.VenueId.Should().Be(VenueId);
    }

    [Theory]
    [InlineData(FixtureStatus.Scheduled, FixtureStatus.Postponed)]
    [InlineData(FixtureStatus.Scheduled, FixtureStatus.Cancelled)]
    [InlineData(FixtureStatus.Scheduled, FixtureStatus.Finished)]
    [InlineData(FixtureStatus.Postponed, FixtureStatus.Scheduled)]
    [InlineData(FixtureStatus.Postponed, FixtureStatus.Finished)]
    public void ChangeStatus_WithAllowedTransition_ShouldSucceed(FixtureStatus from, FixtureStatus to)
    {
        var fixture = CreateManualFixture();
        fixture.ChangeStatus(from).IsSuccess.Should().BeTrue();

        var result = fixture.ChangeStatus(to);

        result.IsSuccess.Should().BeTrue();
        fixture.Status.Should().Be(to);
    }

    [Theory]
    [InlineData(FixtureStatus.Cancelled, FixtureStatus.Scheduled)]
    [InlineData(FixtureStatus.Finished, FixtureStatus.Scheduled)]
    [InlineData(FixtureStatus.Finished, FixtureStatus.Postponed)]
    public void ChangeStatus_WithDisallowedTransition_ShouldFail(FixtureStatus from, FixtureStatus to)
    {
        var fixture = CreateManualFixture();
        fixture.ChangeStatus(from).IsSuccess.Should().BeTrue();

        var result = fixture.ChangeStatus(to);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOneOf(
            Errors.Fixture.Finished,
            Errors.Fixture.InvalidStatusTransition);
    }

    [Fact]
    public void FinishedFixture_ShouldBlockBusinessMutations()
    {
        var fixture = CreateFinishedFixture();

        fixture.AssignVenue(VenueId).Error.Should().Be(Errors.Fixture.Finished);
        fixture.SyncExternalData(SportId, LeagueId, SeasonId, "Updated", StartDate).Error.Should().Be(Errors.Fixture.Finished);
        fixture.UpdateManually(SportId, LeagueId, "Updated", StartDate).Error.Should().Be(Errors.Fixture.Finished);
        fixture.ChangeStatus(FixtureStatus.Scheduled).Error.Should().Be(Errors.Fixture.Finished);
    }

    [Fact]
    public void FinishedFixture_ShouldAllowAdministrativeDelete()
    {
        var fixture = CreateFinishedFixture();

        var result = fixture.Delete();

        result.IsSuccess.Should().BeTrue();
        fixture.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Delete_ShouldBeIdempotent()
    {
        var fixture = CreateManualFixture();

        fixture.Delete().IsSuccess.Should().BeTrue();
        var secondDelete = fixture.Delete();

        secondDelete.IsSuccess.Should().BeTrue();
        fixture.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void DeletedFixture_ShouldBlockBusinessMutations()
    {
        var fixture = CreateManualFixture();
        fixture.Delete().IsSuccess.Should().BeTrue();

        fixture.AssignVenue(VenueId).Error.Should().Be(Errors.Fixture.Deleted);
        fixture.SyncExternalData(SportId, LeagueId, SeasonId, "Updated", StartDate).Error.Should().Be(Errors.Fixture.Deleted);
        fixture.UpdateManually(SportId, LeagueId, "Updated", StartDate).Error.Should().Be(Errors.Fixture.Deleted);
        fixture.ChangeStatus(FixtureStatus.Postponed).Error.Should().Be(Errors.Fixture.Deleted);
    }

    private static Fixture CreateManualFixture()
    {
        return Fixture.CreateManually(SportId, LeagueId, SeasonId, "Home vs Away", StartDate).Value;
    }

    private static Fixture CreateProviderFixture()
    {
        return Fixture.CreateFromProvider(
            SportId,
            LeagueId,
            SeasonId,
            "Home vs Away",
            StartDate,
            "ProviderA",
            "external-123").Value;
    }

    private static Fixture CreateFinishedFixture()
    {
        var fixture = CreateManualFixture();
        fixture.ChangeStatus(FixtureStatus.Finished).IsSuccess.Should().BeTrue();
        return fixture;
    }
}
