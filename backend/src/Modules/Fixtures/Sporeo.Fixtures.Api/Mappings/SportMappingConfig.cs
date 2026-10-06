using Mapster;
using Sporeo.Fixtures.Application.Sports.Commands.CreateSport;
using Sporeo.Fixtures.Application.Sports.Queries.GetActiveSports;
using Sporeo.Fixtures.Contracts.Sports.Requests;
using Sporeo.Fixtures.Contracts.Sports.Responses;

namespace Sporeo.Fixtures.Api.Mappings;

internal sealed class SportMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateSportRequest, CreateSportCommand>()
            .MapToConstructor(true);

        config.NewConfig<ActiveSportReadModel, ActiveSportResponse>()
            .MapToConstructor(true);
    }
}
