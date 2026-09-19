using Mapster;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.Fixtures.Application.Catalogs.Queries.GetCatalog;
using Sporeo.Fixtures.Contracts.Catalogs.Responses;
using Sporeo.Fixtures.Contracts.Common;

namespace Sporeo.Fixtures.Api.Mappings;

public class CatalogMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CatalogLeagueReadModel, CatalogLeagueResponse>()
            .Map(dest => dest.Id, src => src.Id != null ? src.Id.Value : (Guid?)null);

        config.NewConfig<PagedResult<CatalogSportReadModel>, PagedResponse<CatalogSportResponse>>()
            .ConstructUsing(src => new PagedResponse<CatalogSportResponse>(
                src.Items.Adapt<IReadOnlyList<CatalogSportResponse>>(),
                src.TotalCount,
                src.HasNextPage,
                src.Pagination.Page,
                src.Pagination.PageSize));
    }
}
