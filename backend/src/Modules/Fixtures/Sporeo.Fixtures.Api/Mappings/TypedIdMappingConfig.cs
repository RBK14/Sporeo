using Mapster;
using Sporeo.Fixtures.Domain.Fixtures.ValueObjects;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Api.Mappings;

internal sealed class TypedIdMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<FixtureId, Guid>()
            .MapWith(src => src.Value);

        config.NewConfig<VenueId, Guid>()
            .MapWith(src => src.Value);

        config.NewConfig<SportId, Guid>()
            .MapWith(src => src.Value);

        config.NewConfig<LeagueId, Guid>()
            .MapWith(src => src.Value);

        config.NewConfig<SeasonId, Guid>()
            .MapWith(src => src.Value);
    }
}
