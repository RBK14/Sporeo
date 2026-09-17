using FluentAssertions;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;
using LeagueAggregate = Sporeo.Fixtures.Domain.Leagues.League;

namespace Sporeo.Fixtures.Domain.Tests.League;

public class LeagueTests
{
    private static SportId SportId => SportId.FromValue(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    [Fact]
    public void CreateManually_ShouldCreateLeague()
    {
        var result = LeagueAggregate.CreateManually(SportId, "Premier League", "England");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Premier League");
        result.Value.Country.Should().Be("England");
        result.Value.SportId.Should().Be(SportId);
        result.Value.IsManuallyEdited.Should().BeTrue();
        result.Value.ExternalProviderName.Should().BeNull();
        result.Value.ExternalProviderId.Should().BeNull();
    }

    [Fact]
    public void CreateManually_WithEmptyName_ShouldFail()
    {
        var result = LeagueAggregate.CreateManually(SportId, "  ", "England");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.League.EmptyName);
    }

    [Fact]
    public void CreateFromProvider_ShouldPersistProviderMetadata()
    {
        var result = LeagueAggregate.CreateFromProvider(
            SportId,
            "Premier League",
            "England",
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
        var result = LeagueAggregate.CreateFromProvider(
            SportId,
            "Premier League",
            "England",
            providerName,
            providerId);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void SyncExternalData_FromProviderLeague_ShouldUpdateCoreFields()
    {
        var league = CreateProviderLeague();
        var updatedSportId = SportId.FromValue(Guid.Parse("22222222-2222-2222-2222-222222222222"));

        var result = league.SyncExternalData("Updated League", "Poland", updatedSportId);

        result.IsSuccess.Should().BeTrue();
        league.Name.Should().Be("Updated League");
        league.Country.Should().Be("Poland");
        league.SportId.Should().Be(updatedSportId);
    }

    [Fact]
    public void SyncExternalData_WhenManuallyEdited_ShouldFail()
    {
        var league = CreateManualLeague();

        var result = league.SyncExternalData("Provider Update", "England", SportId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.League.LockedForSync);
    }

    [Fact]
    public void UpdateManually_ShouldUpdateCoreFieldsAndLockSync()
    {
        var league = CreateProviderLeague();
        var updatedSportId = SportId.FromValue(Guid.Parse("22222222-2222-2222-2222-222222222222"));

        var result = league.UpdateManually("Manual League", "Spain", updatedSportId);

        result.IsSuccess.Should().BeTrue();
        league.Name.Should().Be("Manual League");
        league.Country.Should().Be("Spain");
        league.SportId.Should().Be(updatedSportId);
        league.IsManuallyEdited.Should().BeTrue();
        league.SyncExternalData("Provider Update", "England", SportId).Error.Should().Be(Errors.League.LockedForSync);
    }

    [Fact]
    public void Delete_ShouldBeIdempotent()
    {
        var league = CreateManualLeague();

        league.Delete().IsSuccess.Should().BeTrue();
        var secondDelete = league.Delete();

        secondDelete.IsSuccess.Should().BeTrue();
        league.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void DeletedLeague_ShouldBlockBusinessMutations()
    {
        var league = CreateManualLeague();
        league.Delete().IsSuccess.Should().BeTrue();

        league.SyncExternalData("Updated", "England", SportId).Error.Should().Be(Errors.League.Deleted);
        league.UpdateManually("Updated", "England", SportId).Error.Should().Be(Errors.League.Deleted);
    }

    private static LeagueAggregate CreateManualLeague()
    {
        return LeagueAggregate.CreateManually(SportId, "Premier League", "England").Value;
    }

    private static LeagueAggregate CreateProviderLeague()
    {
        return LeagueAggregate.CreateFromProvider(
            SportId,
            "Premier League",
            "England",
            "ProviderA",
            "external-123").Value;
    }
}
