using Mapster;
using Sporeo.Fixtures.Application.Leagues.Commands.CreateLeague;
using Sporeo.Fixtures.Contracts.Leagues.Requests;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Api.Mappings;

internal sealed class LeagueMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateLeagueRequest, CreateLeagueCommand>()
            .MapWith(src => new CreateLeagueCommand(
                SportId.FromValue(src.SportId),
                src.Name,
                src.Country));
    }
}
