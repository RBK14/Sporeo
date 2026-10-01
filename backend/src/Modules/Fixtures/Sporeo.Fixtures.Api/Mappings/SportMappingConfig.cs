using Mapster;
using Sporeo.Fixtures.Application.Sports.Commands.CreateSport;
using Sporeo.Fixtures.Contracts.Sports.Requests;

namespace Sporeo.Fixtures.Api.Mappings;

internal sealed class SportMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateSportRequest, CreateSportCommand>()
            .MapToConstructor(true);
    }
}
