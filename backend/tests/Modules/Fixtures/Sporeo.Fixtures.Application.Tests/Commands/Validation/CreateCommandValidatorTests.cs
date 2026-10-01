using FluentAssertions;
using Sporeo.Fixtures.Application.Common;
using Sporeo.Fixtures.Application.Leagues.Commands.CreateLeague;
using Sporeo.Fixtures.Application.Seasons.Commands.CreateSeason;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Tests.Commands.Validation;

public sealed class CreateCommandValidatorTests
{
    [Fact]
    public async Task CreateLeague_WhenSportIdIsNull_ShouldReturnSportIdRequiredCode()
    {
        var validator = new CreateLeagueCommandValidator();
        var command = new CreateLeagueCommand(null!, "Premier League", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == Errors.League.SportIdRequired.Code);
        result.Errors.Should().NotContain(e => e.ErrorCode == "Sport.NotFound");
    }

    [Fact]
    public async Task CreateSeason_WhenLeagueIdIsNull_ShouldReturnLeagueIdRequiredCode()
    {
        var validator = new CreateSeasonCommandValidator();
        var command = new CreateSeasonCommand(null!, "2025-2026");

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == Errors.Season.LeagueIdRequired.Code);
        result.Errors.Should().NotContain(e => e.ErrorCode == "League.NotFound");
    }

    [Fact]
    public async Task CreateLeague_WithValidSportId_ShouldSucceed()
    {
        var validator = new CreateLeagueCommandValidator();
        var command = new CreateLeagueCommand(SportId.New(), "Premier League", "England");

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateSeason_WithValidLeagueId_ShouldSucceed()
    {
        var validator = new CreateSeasonCommandValidator();
        var command = new CreateSeasonCommand(LeagueId.New(), "2025-2026");

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
