using Mapster;
using Sporeo.Fixtures.Application.Catalogs.Commands.UpdateMonitoring;
using Sporeo.Fixtures.Application.Catalogs.Queries.GetCatalog;
using Sporeo.Fixtures.Contracts.Catalogs.Requests;
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

        config.NewConfig<UpdateMonitoringLeagueRequest, UpdateMonitoringLeagueDto>()
            .MapToConstructor(true);

        config.NewConfig<UpdateMonitoringSportRequest, UpdateMonitoringSportDto>()
            .Map(dest => dest.Leagues, src => src.Leagues)
            .MapToConstructor(true);

        config.NewConfig<UpdateMonitoringRequest, UpdateMonitoringCommand>()
            .Map(dest => dest.Sports, src => src.Sports)
            .MapToConstructor(true);
    }
}
