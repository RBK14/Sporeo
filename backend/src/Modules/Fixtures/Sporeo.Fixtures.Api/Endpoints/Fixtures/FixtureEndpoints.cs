using MapsterMapper;
using MediatR;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.Fixtures.Api.Extensions;
using Sporeo.Fixtures.Application.Fixtures.Queries.Common;
using Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtureDetails;
using Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtures;
using Sporeo.Fixtures.Application.Fixtures.Queries.GetNearbyFixtures;
using Sporeo.Fixtures.Contracts.Common;
using Sporeo.Fixtures.Contracts.Fixtures.Requests;
using Sporeo.Fixtures.Contracts.Fixtures.Responses;
using Sporeo.Fixtures.Domain.Fixtures.ValueObjects;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Api.Endpoints.Fixtures;

public sealed class FixtureEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/fixtures")
            .WithTags("Fixtures");

        group.MapGet("", GetFixturesAsync)
            .WithName("GetFixtures")
            .WithSummary("Gets a paged list of fixtures.")
            .WithDescription("Retrieves a paginated list of fixtures matching optional sport, league, and date filters.")
            .Produces<PagedResponse<FixtureListItemResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("nearby", GetNearbyFixturesAsync)
            .WithName("GetNearbyFixtures")
            .WithSummary("Gets a paged list of fixtures near a geographic point.")
            .WithDescription("Retrieves a paginated list of fixtures within the specified radius of a geographic origin, " +
                "optionally filtered by sport, league, and date.")
            .Produces<PagedResponse<NearbyFixtureListItemResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("{fixtureId:guid}", GetFixtureDetailsAsync)
            .WithName("GetFixtureDetails")
            .WithSummary("Gets detailed information for a single fixture.")
            .WithDescription("Retrieves detailed fixture data including optional venue, league, and season relations.")
            .Produces<FixtureDetailsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> GetFixturesAsync(
        [AsParameters] GetFixturesRequest request,
        ISender sender,
        IMapper mapper,
        CancellationToken cancellationToken)
    {
        var pagination = new PaginationParams(request.PageNumber, request.PageSize);
        var filters = new FixtureFilters(
            request.SportId.HasValue ? SportId.FromValue(request.SportId.Value) : null,
            request.LeagueId.HasValue ? LeagueId.FromValue(request.LeagueId.Value) : null,
            request.DateFrom,
            request.DateTo);
        var query = new GetFixturesQuery(pagination, filters);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        var response = mapper.Map<PagedResponse<FixtureListItemResponse>>(result.Value);

        return Results.Ok(response);
    }

    private static async Task<IResult> GetNearbyFixturesAsync(
        [AsParameters] GetNearbyFixturesRequest request,
        ISender sender,
        IMapper mapper,
        CancellationToken cancellationToken)
    {
        var pagination = new PaginationParams(request.PageNumber, request.PageSize);
        var filters = new FixtureFilters(
            request.SportId.HasValue ? SportId.FromValue(request.SportId.Value) : null,
            request.LeagueId.HasValue ? LeagueId.FromValue(request.LeagueId.Value) : null,
            request.DateFrom,
            request.DateTo);
        var query = new GetNearbyFixturesQuery(
            request.Latitude,
            request.Longitude,
            request.RadiusInMeters,
            pagination,
            filters);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        var response = mapper.Map<PagedResponse<NearbyFixtureListItemResponse>>(result.Value);

        return Results.Ok(response);
    }

    private static async Task<IResult> GetFixtureDetailsAsync(
        Guid fixtureId,
        ISender sender,
        IMapper mapper,
        CancellationToken cancellationToken)
    {
        var query = new GetFixtureDetailsQuery(FixtureId.FromValue(fixtureId));

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        var response = mapper.Map<FixtureDetailsResponse>(result.Value);

        return Results.Ok(response);
    }
}
