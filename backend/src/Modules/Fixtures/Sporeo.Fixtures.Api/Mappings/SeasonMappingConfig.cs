using Mapster;
using Sporeo.Fixtures.Application.Seasons.Commands.CreateSeason;
using Sporeo.Fixtures.Contracts.Seasons.Requests;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;

namespace Sporeo.Fixtures.Api.Mappings;

internal sealed class SeasonMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateSeasonRequest, CreateSeasonCommand>()
            .MapWith(src => new CreateSeasonCommand(
                LeagueId.FromValue(src.LeagueId),
                src.Name));
    }
}
