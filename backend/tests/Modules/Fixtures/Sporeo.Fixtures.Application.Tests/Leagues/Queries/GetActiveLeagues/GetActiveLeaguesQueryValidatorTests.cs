using FluentAssertions;
using Sporeo.Fixtures.Application.Leagues.Queries.GetActiveLeagues;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Application.Tests.Leagues.Queries.GetActiveLeagues;

public class GetActiveLeaguesQueryValidatorTests
{
    private readonly GetActiveLeaguesQueryValidator _validator = new();

    [Fact]
    public void Validate_WithNullSportId_ShouldSucceed()
    {
        var query = new GetActiveLeaguesQuery(SportId: null);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithValidSportId_ShouldSucceed()
    {
        var query = new GetActiveLeaguesQuery(SportId.New());

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }
}
