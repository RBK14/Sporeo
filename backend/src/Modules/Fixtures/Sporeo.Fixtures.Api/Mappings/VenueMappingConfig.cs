using Mapster;
using Sporeo.Fixtures.Application.Venues.Queries.GetVenueDetails;
using Sporeo.Fixtures.Contracts.Venues.Responses;

namespace Sporeo.Fixtures.Api.Mappings;

internal sealed class VenueMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<VenueDetailsReadModel, VenueDetailsResponse>()
            .MapToConstructor(true);
    }
}
