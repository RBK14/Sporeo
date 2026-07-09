using FluentAssertions;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;
using SeasonAggregate = Sporeo.Fixtures.Domain.Seasons.Season;

namespace Sporeo.Fixtures.Domain.Tests.Seasons;

public class SeasonTests
{
    private static readonly DateTimeOffset StartDate = new(2025, 8, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EndDate = new(2026, 6, 30, 0, 0, 0, TimeSpan.Zero);

    private static LeagueId LeagueId => LeagueId.FromValue(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    [Fact]
    public void CreateManually_ShouldCreateSeason()
    {
        var result = SeasonAggregate.CreateManually(LeagueId, "2025/2026", StartDate, EndDate);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("2025/2026");
        result.Value.LeagueId.Should().Be(LeagueId);
        result.Value.StartDate.Should().Be(StartDate);
        result.Value.EndDate.Should().Be(EndDate);
        result.Value.IsManuallyEdited.Should().BeTrue();
        result.Value.IsCurrent.Should().BeFalse();
    }

    [Fact]
    public void CreateManually_WithEmptyName_ShouldFail()
    {
        var result = SeasonAggregate.CreateManually(LeagueId, "  ", StartDate, EndDate);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Season.EmptyName);
    }

    [Fact]
    public void CreateManually_WithInvalidDateRange_ShouldFail()
    {
        var result = SeasonAggregate.CreateManually(LeagueId, "2025/2026", EndDate, StartDate);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Season.InvalidDateRange);
    }

    [Fact]
    public void CreateFromProvider_ShouldPersistProviderMetadata()
    {
        var result = SeasonAggregate.CreateFromProvider(
            LeagueId,
            "2025/2026",
            StartDate,
            EndDate,
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
        var result = SeasonAggregate.CreateFromProvider(
            LeagueId,
            "2025/2026",
            StartDate,
            EndDate,
            providerName,
            providerId);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void SyncExternalData_FromProviderSeason_ShouldUpdateCoreFields()
    {
        var season = CreateProviderSeason();
        var updatedStartDate = StartDate.AddMonths(1);
        var updatedEndDate = EndDate.AddMonths(1);

        var result = season.SyncExternalData("Updated Season", updatedStartDate, updatedEndDate);

        result.IsSuccess.Should().BeTrue();
        season.Name.Should().Be("Updated Season");
        season.StartDate.Should().Be(updatedStartDate);
        season.EndDate.Should().Be(updatedEndDate);
    }

    [Fact]
    public void SyncExternalData_WhenManuallyEdited_ShouldFail()
    {
        var season = CreateManualSeason();

        var result = season.SyncExternalData("Provider Update", StartDate, EndDate);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Season.LockedForSync);
    }

    [Fact]
    public void UpdateManually_ShouldUpdateCoreFieldsAndLockSync()
    {
        var season = CreateProviderSeason();
        var updatedStartDate = StartDate.AddMonths(2);
        var updatedEndDate = EndDate.AddMonths(2);

        var result = season.UpdateManually("Manual Season", updatedStartDate, updatedEndDate);

        result.IsSuccess.Should().BeTrue();
        season.Name.Should().Be("Manual Season");
        season.StartDate.Should().Be(updatedStartDate);
        season.EndDate.Should().Be(updatedEndDate);
        season.IsManuallyEdited.Should().BeTrue();
        season.SyncExternalData("Provider Update", StartDate, EndDate).Error.Should().Be(Errors.Season.LockedForSync);
    }

    [Fact]
    public void MarkAsCurrent_ShouldSetIsCurrent()
    {
        var season = CreateManualSeason();

        var result = season.MarkAsCurrent();

        result.IsSuccess.Should().BeTrue();
        season.IsCurrent.Should().BeTrue();
    }

    [Fact]
    public void UnmarkAsCurrent_ShouldClearIsCurrent()
    {
        var season = CreateManualSeason();
        season.MarkAsCurrent().IsSuccess.Should().BeTrue();

        var result = season.UnmarkAsCurrent();

        result.IsSuccess.Should().BeTrue();
        season.IsCurrent.Should().BeFalse();
    }

    [Fact]
    public void Delete_ShouldBeIdempotent()
    {
        var season = CreateManualSeason();

        season.Delete().IsSuccess.Should().BeTrue();
        var secondDelete = season.Delete();

        secondDelete.IsSuccess.Should().BeTrue();
        season.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void DeletedSeason_ShouldBlockBusinessMutations()
    {
        var season = CreateManualSeason();
        season.Delete().IsSuccess.Should().BeTrue();

        season.SyncExternalData("Updated", StartDate, EndDate).Error.Should().Be(Errors.Season.Deleted);
        season.UpdateManually("Updated", StartDate, EndDate).Error.Should().Be(Errors.Season.Deleted);
        season.MarkAsCurrent().Error.Should().Be(Errors.Season.Deleted);
        season.UnmarkAsCurrent().Error.Should().Be(Errors.Season.Deleted);
    }

    private static SeasonAggregate CreateManualSeason()
    {
        return SeasonAggregate.CreateManually(LeagueId, "2025/2026", StartDate, EndDate).Value;
    }

    private static SeasonAggregate CreateProviderSeason()
    {
        return SeasonAggregate.CreateFromProvider(
            LeagueId,
            "2025/2026",
            StartDate,
            EndDate,
            "ProviderA",
            "external-123").Value;
    }
}
