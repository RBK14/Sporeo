using FluentAssertions;
using Mapster;
using Sporeo.Fixtures.Application.Leagues.Queries.GetActiveLeagues;
using Sporeo.Fixtures.Application.Sports.Queries.GetActiveSports;
using Sporeo.Fixtures.Contracts.Leagues.Responses;
using Sporeo.Fixtures.Contracts.Sports.Responses;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Api.Tests.Mappings;

public class ActiveCatalogMappingConfigTests
{
    private static readonly TypeAdapterConfig Config = CreateConfig();

    private static TypeAdapterConfig CreateConfig()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(DependencyInjection).Assembly);
        return config;
    }

    [Fact]
    public void ActiveSports_ShouldMapIdAndName()
    {
        var sportId = SportId.New();
        IReadOnlyList<ActiveSportReadModel> source = [new ActiveSportReadModel(sportId, "Football")];

        var response = source.Adapt<List<ActiveSportResponse>>(Config);

        response.Should().ContainSingle()
            .Which.Should().Be(new ActiveSportResponse(sportId.Value, "Football"));
    }

    [Fact]
    public void ActiveLeague_WithCurrentSeason_ShouldMapNestedSportAndSeason()
    {
        var leagueId = LeagueId.New();
        var sportId = SportId.New();
        var seasonId = SeasonId.New();
        var source = new ActiveLeagueReadModel(leagueId, "Ekstraklasa", "PL", sportId, "Football", seasonId, "2025/26");

        var response = source.Adapt<ActiveLeagueResponse>(Config);

        response.Id.Should().Be(leagueId.Value);
        response.Name.Should().Be("Ekstraklasa");
        response.Country.Should().Be("PL");
        response.Sport.Should().Be(new ActiveLeagueSportResponse(sportId.Value, "Football"));
        response.CurrentSeason.Should().Be(new ActiveLeagueSeasonResponse(seasonId.Value, "2025/26"));
    }

    [Fact]
    public void ActiveLeague_WithoutCurrentSeason_ShouldMapNullSeason()
    {
        var source = new ActiveLeagueReadModel(LeagueId.New(), "I Liga", null, SportId.New(), "Football", null, null);

        var response = source.Adapt<ActiveLeagueResponse>(Config);

        response.Country.Should().BeNull();
        response.CurrentSeason.Should().BeNull();
    }
}
