using Mapster;
using Sporeo.Fixtures.Application.Catalogs.Queries.GetCatalog;
using Sporeo.Fixtures.Contracts.Catalogs.Responses;

namespace Sporeo.Fixtures.Api.Mappings;

internal sealed class CatalogMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CatalogLeagueReadModel, CatalogLeagueResponse>()
            .MapWith(src => new CatalogLeagueResponse(
                src.Id != null ? src.Id.Value : null,
                src.ProviderId,
                src.ProviderName,
                src.Name,
                src.IsMonitored));

        config.NewConfig<CatalogSportReadModel, CatalogSportResponse>()
            .Map(dest => dest.Leagues, src => src.Leagues)
            .MapToConstructor(true);
    }
}
