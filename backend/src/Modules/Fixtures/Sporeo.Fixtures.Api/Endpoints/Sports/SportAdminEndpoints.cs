using MapsterMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Sporeo.Fixtures.Api.Extensions;
using Sporeo.Fixtures.Application.Sports.Commands.CreateSport;
using Sporeo.Fixtures.Contracts.Sports.Requests;
using Sporeo.Fixtures.Contracts.Sports.Responses;

namespace Sporeo.Fixtures.Api.Endpoints.Sports;

public sealed class SportAdminEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/admin/sports")
            .WithTags("Sports");
        // todo: RequireAuthorization() when authentication is wired up for admin routes.

        group.MapPost("", CreateSportAsync)
            .WithName("CreateSport")
            .WithSummary("Creates a new sport.")
            .WithDescription("Creates a sport aggregate with the specified display name.")
            .Produces<CreateSportResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> CreateSportAsync(
        [FromBody] CreateSportRequest request,
        ISender sender,
        IMapper mapper,
        CancellationToken cancellationToken)
    {
        var command = mapper.Map<CreateSportCommand>(request);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        var response = new CreateSportResponse(result.Value.Value);

        return Results.Created($"api/v1/admin/sports/{result.Value.Value}", response);
    }
}
