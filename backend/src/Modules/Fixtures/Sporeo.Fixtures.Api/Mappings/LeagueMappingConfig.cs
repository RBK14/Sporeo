using Mapster;
using Sporeo.Fixtures.Application.Leagues.Commands.CreateLeague;
using Sporeo.Fixtures.Application.Leagues.Queries.GetActiveLeagues;
using Sporeo.Fixtures.Contracts.Leagues.Requests;
using Sporeo.Fixtures.Contracts.Leagues.Responses;
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

        config.NewConfig<ActiveLeagueReadModel, ActiveLeagueResponse>()
            .MapWith(src => new ActiveLeagueResponse(
                src.Id.Value,
                src.Name,
                src.Country,
                new ActiveLeagueSportResponse(src.SportId.Value, src.SportName),
                src.CurrentSeasonId == null
                    ? null
                    : new ActiveLeagueSeasonResponse(src.CurrentSeasonId.Value, src.CurrentSeasonName!)));
    }
}
