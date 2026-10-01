using FluentAssertions;
using Mapster;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.Fixtures.Application.Catalogs.Queries.GetCatalog;
using Sporeo.Fixtures.Contracts.Catalogs.Responses;
using Sporeo.Fixtures.Contracts.Common;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;

namespace Sporeo.Fixtures.Api.Tests.Mappings;

public class CatalogMappingConfigTests
{
    private static readonly TypeAdapterConfig Config = CreateConfig();

    private static TypeAdapterConfig CreateConfig()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(DependencyInjection).Assembly);
        return config;
    }

    [Fact]
    public void PagedResult_ShouldMapPageNumberAndPageSizeFromPagination()
    {
        var leagueId = LeagueId.New();
        var pagination = new PaginationParams(2, 5);
        var source = new PagedResult<CatalogSportReadModel>(
            [
                new CatalogSportReadModel(
                    null,
                    "1",
                    "TheSportsDB",
                    "Soccer",
                    [
                        new CatalogLeagueReadModel(
                            leagueId,
                            "4328",
                            "TheSportsDB",
                            "English Premier League",
                            true),
                        new CatalogLeagueReadModel(
                            null,
                            "4335",
                            "TheSportsDB",
                            "Spanish La Liga",
                            false)
                    ])
            ],
            totalCount: 12,
            pagination);

        var response = source.Adapt<PagedResponse<CatalogSportResponse>>(Config);

        response.PageNumber.Should().Be(2);
        response.PageSize.Should().Be(5);
        response.TotalCount.Should().Be(12);
        response.HasNextPage.Should().BeTrue();
        response.Items.Should().ContainSingle();

        var sport = response.Items[0];
        sport.ProviderId.Should().Be("1");
        sport.Name.Should().Be("Soccer");
        sport.Leagues.Should().HaveCount(2);
        sport.Leagues[0].Id.Should().Be(leagueId.Value);
        sport.Leagues[0].IsMonitored.Should().BeTrue();
        sport.Leagues[1].Id.Should().BeNull();
        sport.Leagues[1].IsMonitored.Should().BeFalse();
    }
}
