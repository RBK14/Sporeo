using Mapster;
using Sporeo.Fixtures.Application.Catalogs.Commands.UpdateMonitoring;
using Sporeo.Fixtures.Application.Catalogs.Queries.GetCatalog;
using Sporeo.Fixtures.Contracts.Catalogs.Requests;
using Sporeo.Fixtures.Contracts.Catalogs.Responses;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

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
            .MapWith(src => new CatalogSportResponse(
                src.Id != null ? src.Id.Value : null,
                src.ProviderId,
                src.ProviderName,
                src.Name,
                src.Leagues.Adapt<IReadOnlyList<CatalogLeagueResponse>>(config)));

        config.NewConfig<UpdateMonitoringLeagueRequest, UpdateMonitoringLeagueDto>()
            .MapWith(src => new UpdateMonitoringLeagueDto(
                src.Id.HasValue ? LeagueId.FromValue(src.Id.Value) : null,
                src.ProviderId,
                src.ProviderName,
                src.IsMonitored));

        config.NewConfig<UpdateMonitoringSportRequest, UpdateMonitoringSportDto>()
            .MapWith(src => new UpdateMonitoringSportDto(
                src.Id.HasValue ? SportId.FromValue(src.Id.Value) : null,
                src.ProviderId,
                src.ProviderName,
                src.Leagues.Adapt<IReadOnlyList<UpdateMonitoringLeagueDto>>(config)));

        config.NewConfig<UpdateMonitoringRequest, UpdateMonitoringCommand>()
            .Map(dest => dest.Sports, src => src.Sports)
            .MapToConstructor(true);
    }
}
