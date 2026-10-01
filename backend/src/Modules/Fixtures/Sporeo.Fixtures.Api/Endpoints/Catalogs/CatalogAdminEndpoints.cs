using MapsterMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.Fixtures.Api.Extensions;
using Sporeo.Fixtures.Application.Catalogs.Commands.UpdateMonitoring;
using Sporeo.Fixtures.Application.Catalogs.Queries.GetCatalog;
using Sporeo.Fixtures.Contracts.Catalogs.Requests;
using Sporeo.Fixtures.Contracts.Catalogs.Responses;
using Sporeo.Fixtures.Contracts.Common;

namespace Sporeo.Fixtures.Api.Endpoints.Catalogs;

public sealed class CatalogAdminEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/admin/catalogs")
            .WithTags("Catalogs");
        // todo: RequireAuthorization() when authentication is wired up for admin routes.

        group.MapGet("", GetCatalogAsync)
            .WithName("GetCatalog")
            .WithSummary("Gets a paged catalog of sports and leagues.")
            .WithDescription("Retrieves a cached, paginated list of sports and their associated leagues from the external provider. " +
                "Used by administrators to browse available competitions.")
            .Produces<PagedResponse<CatalogSportResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPut("monitoring", UpdateMonitoringAsync)
            .WithName("UpdateMonitoring")
            .WithSummary("Updates monitoring status for sports and leagues.")
            .WithDescription("Creates or updates sports and leagues from the external provider catalog, applying the specified monitoring status. " +
                "Triggers outbox domain events for newly enabled leagues.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> GetCatalogAsync(
        [AsParameters] GetCatalogRequest request,
        ISender sender,
        IMapper mapper,
        CancellationToken cancellationToken)
    {
        var paginationParams = new PaginationParams(request.PageNumber, request.PageSize);
        var query = new GetCatalogQuery(paginationParams);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        var response = mapper.Map<PagedResponse<CatalogSportResponse>>(result.Value);

        return Results.Ok(response);
    }

    private static async Task<IResult> UpdateMonitoringAsync(
        [FromBody] UpdateMonitoringRequest request,
        ISender sender,
        IMapper mapper,
        CancellationToken cancellationToken)
    {
        var command = mapper.Map<UpdateMonitoringCommand>(request);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemResult();
        }

        return Results.NoContent();
    }
}
