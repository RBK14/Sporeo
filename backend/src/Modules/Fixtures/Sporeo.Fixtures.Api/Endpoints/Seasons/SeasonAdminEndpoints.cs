using MapsterMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Sporeo.Fixtures.Api.Extensions;
using Sporeo.Fixtures.Application.Seasons.Commands.CreateSeason;
using Sporeo.Fixtures.Contracts.Seasons.Requests;
using Sporeo.Fixtures.Contracts.Seasons.Responses;

namespace Sporeo.Fixtures.Api.Endpoints.Seasons;

public sealed class SeasonAdminEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/admin/seasons")
            .WithTags("Seasons");
        // todo: RequireAuthorization() when authentication is wired up for admin routes.

        group.MapPost("", CreateSeasonAsync)
            .WithName("CreateSeason")
            .WithSummary("Creates a new season.")
            .WithDescription("Creates a season aggregate under the specified league.")
            .Produces<CreateSeasonResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> CreateSeasonAsync(
        [FromBody] CreateSeasonRequest request,
        ISender sender,
        IMapper mapper,
        CancellationToken cancellationToken)
    {
        var command = mapper.Map<CreateSeasonCommand>(request);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        var response = new CreateSeasonResponse(result.Value.Value);

        return Results.Created($"/api/v1/admin/seasons/{result.Value.Value}", response);
    }
}
