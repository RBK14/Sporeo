using MapsterMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Sporeo.Fixtures.Api.Extensions;
using Sporeo.Fixtures.Application.Leagues.Commands.CreateLeague;
using Sporeo.Fixtures.Contracts.Leagues.Requests;
using Sporeo.Fixtures.Contracts.Leagues.Responses;

namespace Sporeo.Fixtures.Api.Endpoints.Leagues;

public sealed class LeagueAdminEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/admin/leagues")
            .WithTags("Leagues");
        // todo: RequireAuthorization() when authentication is wired up for admin routes.

        group.MapPost("", CreateLeagueAsync)
            .WithName("CreateLeague")
            .WithSummary("Creates a new league.")
            .WithDescription("Creates a league aggregate under the specified sport with an optional country association.")
            .Produces<CreateLeagueResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> CreateLeagueAsync(
        [FromBody] CreateLeagueRequest request,
        ISender sender,
        IMapper mapper,
        CancellationToken cancellationToken)
    {
        var command = mapper.Map<CreateLeagueCommand>(request);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        var response = new CreateLeagueResponse(result.Value.Value);

        return Results.Created($"/api/v1/admin/leagues/{result.Value.Value}", response);
    }
}
