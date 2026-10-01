using Mapster;
using Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtureDetails;
using Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtures;
using Sporeo.Fixtures.Application.Fixtures.Queries.GetNearbyFixtures;
using Sporeo.Fixtures.Contracts.Fixtures.Responses;

namespace Sporeo.Fixtures.Api.Mappings;

internal sealed class FixtureMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<FixtureListItemReadModel, FixtureListItemResponse>()
            .MapToConstructor(true);

        config.NewConfig<NearbyFixtureListItemReadModel, NearbyFixtureListItemResponse>()
            .MapToConstructor(true);

        config.NewConfig<FixtureSportReadModel, FixtureSportResponse>()
            .MapToConstructor(true);

        config.NewConfig<FixtureLeagueReadModel, FixtureLeagueResponse>()
            .MapToConstructor(true);

        config.NewConfig<FixtureSeasonReadModel, FixtureSeasonResponse>()
            .MapToConstructor(true);

        config.NewConfig<FixtureVenueReadModel, FixtureVenueResponse>()
            .MapToConstructor(true);

        config.NewConfig<FixtureDetailsReadModel, FixtureDetailsResponse>()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .MapToConstructor(true);
    }
}
