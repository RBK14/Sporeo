using FluentAssertions;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using SeasonAggregate = Sporeo.Fixtures.Domain.Seasons.Season;

namespace Sporeo.Fixtures.Domain.Tests.Seasons;

public class SeasonTests
{
    private static LeagueId LeagueId => LeagueId.FromValue(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    [Fact]
    public void Create_WithOnlyName_ShouldNotRequireProviderMetadata()
    {
        var result = SeasonAggregate.Create(LeagueId, "2025/2026");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("2025/2026");
        result.Value.LeagueId.Should().Be(LeagueId);
        result.Value.ExternalProviderName.Should().BeNull();
        result.Value.ExternalProviderId.Should().BeNull();
        result.Value.IsCurrent.Should().BeFalse();
        result.Value.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_WithPartialProviderMetadata_ShouldSucceed()
    {
        var result = SeasonAggregate.Create(LeagueId, "2025/2026", "ProviderA", null);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalProviderName.Should().Be("ProviderA");
        result.Value.ExternalProviderId.Should().BeNull();
    }

    [Fact]
    public void Create_WithOptionalProviderMetadata_ShouldPersistProviderFields()
    {
        var result = SeasonAggregate.Create(LeagueId, "2025/2026", "ProviderA", "external-123");

        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalProviderName.Should().Be("ProviderA");
        result.Value.ExternalProviderId.Should().Be("external-123");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldFail()
    {
        var result = SeasonAggregate.Create(LeagueId, "  ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Season.EmptyName);
    }

    [Fact]
    public void Update_ShouldChangeName()
    {
        var season = SeasonAggregate.Create(LeagueId, "2025/2026").Value;

        var result = season.Update("2026/2027");

        result.IsSuccess.Should().BeTrue();
        season.Name.Should().Be("2026/2027");
    }

    [Fact]
    public void Update_WithEmptyName_ShouldFail()
    {
        var season = SeasonAggregate.Create(LeagueId, "2025/2026").Value;

        var result = season.Update("  ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Season.EmptyName);
        season.Name.Should().Be("2025/2026");
    }

    [Fact]
    public void MarkAsCurrent_ShouldSetIsCurrent()
    {
        var season = SeasonAggregate.Create(LeagueId, "2025/2026").Value;

        var result = season.MarkAsCurrent();

        result.IsSuccess.Should().BeTrue();
        season.IsCurrent.Should().BeTrue();
    }

    [Fact]
    public void UnmarkAsCurrent_ShouldClearIsCurrent()
    {
        var season = SeasonAggregate.Create(LeagueId, "2025/2026").Value;
        season.MarkAsCurrent().IsSuccess.Should().BeTrue();

        var result = season.UnmarkAsCurrent();

        result.IsSuccess.Should().BeTrue();
        season.IsCurrent.Should().BeFalse();
    }

    [Fact]
    public void Delete_ShouldBeIdempotent()
    {
        var season = SeasonAggregate.Create(LeagueId, "2025/2026").Value;

        season.Delete().IsSuccess.Should().BeTrue();
        var secondDelete = season.Delete();

        secondDelete.IsSuccess.Should().BeTrue();
        season.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void DeletedSeason_ShouldBlockBusinessMutations()
    {
        var season = SeasonAggregate.Create(LeagueId, "2025/2026").Value;
        season.Delete().IsSuccess.Should().BeTrue();

        season.Update("Updated").Error.Should().Be(Errors.Season.Deleted);
        season.MarkAsCurrent().Error.Should().Be(Errors.Season.Deleted);
        season.UnmarkAsCurrent().Error.Should().Be(Errors.Season.Deleted);
        season.Name.Should().Be("2025/2026");
    }
}
