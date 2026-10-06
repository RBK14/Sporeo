using MapsterMapper;
using MediatR;
using Sporeo.Fixtures.Api.Extensions;
using Sporeo.Fixtures.Application.Sports.Queries.GetActiveSports;
using Sporeo.Fixtures.Contracts.Sports.Responses;

namespace Sporeo.Fixtures.Api.Endpoints.Sports;

public sealed class SportEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/sports")
            .WithTags("Sports");

        group.MapGet("", GetActiveSportsAsync)
            .WithName("GetActiveSports")
            .WithSummary("Gets sports that have monitored leagues.")
            .WithDescription("Retrieves sports that currently have at least one monitored league, ordered by name.")
            .Produces<IReadOnlyList<ActiveSportResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> GetActiveSportsAsync(
        ISender sender,
        IMapper mapper,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetActiveSportsQuery(), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        var response = mapper.Map<List<ActiveSportResponse>>(result.Value);

        return Results.Ok(response);
    }
}
