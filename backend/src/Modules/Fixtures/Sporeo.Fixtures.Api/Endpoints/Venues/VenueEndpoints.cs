using MapsterMapper;
using MediatR;
using Sporeo.Fixtures.Api.Extensions;
using Sporeo.Fixtures.Application.Venues.Queries.GetVenueDetails;
using Sporeo.Fixtures.Contracts.Venues.Responses;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Api.Endpoints.Venues;

public sealed class VenueEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/venues")
            .WithTags("Venues");

        group.MapGet("{venueId:guid}", GetVenueDetailsAsync)
            .WithName("GetVenueDetails")
            .WithSummary("Gets detailed information for a single venue.")
            .WithDescription("Retrieves detailed venue data including optional address and coordinates.")
            .Produces<VenueDetailsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> GetVenueDetailsAsync(
        Guid venueId,
        ISender sender,
        IMapper mapper,
        CancellationToken cancellationToken)
    {
        var query = new GetVenueDetailsQuery(VenueId.FromValue(venueId));

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        var response = mapper.Map<VenueDetailsResponse>(result.Value);

        return Results.Ok(response);
    }
}
