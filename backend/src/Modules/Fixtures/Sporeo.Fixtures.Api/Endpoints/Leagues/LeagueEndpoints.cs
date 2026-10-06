using MapsterMapper;
using MediatR;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Api.Extensions;
using Sporeo.Fixtures.Application.Leagues.Queries.GetActiveLeagues;
using Sporeo.Fixtures.Contracts.Leagues.Requests;
using Sporeo.Fixtures.Contracts.Leagues.Responses;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;
using AppErrors = Sporeo.Fixtures.Application.Common.Errors;

namespace Sporeo.Fixtures.Api.Endpoints.Leagues;

public sealed class LeagueEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/leagues")
            .WithTags("Leagues");

        group.MapGet("", GetActiveLeaguesAsync)
            .WithName("GetActiveLeagues")
            .WithSummary("Gets actively monitored leagues.")
            .WithDescription("Retrieves monitored leagues with their sport and optional current season, " +
                "optionally filtered by sport, ordered by sport name and league name.")
            .Produces<IReadOnlyList<ActiveLeagueResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> GetActiveLeaguesAsync(
        [AsParameters] GetActiveLeaguesRequest request,
        ISender sender,
        IMapper mapper,
        CancellationToken cancellationToken)
    {
        // Typed ids reject Guid.Empty by throwing, so the filter is validated before conversion.
        if (request.SportId == Guid.Empty)
        {
            return new ValidationError([AppErrors.League.InvalidSportId with { PropertyName = "sportId" }])
                .ToProblemResult();
        }

        var query = new GetActiveLeaguesQuery(
            request.SportId.HasValue ? SportId.FromValue(request.SportId.Value) : null);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        var response = mapper.Map<List<ActiveLeagueResponse>>(result.Value);

        return Results.Ok(response);
    }
}
