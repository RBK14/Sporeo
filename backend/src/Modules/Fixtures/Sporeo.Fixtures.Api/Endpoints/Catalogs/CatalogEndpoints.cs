using Mapster;
using MediatR;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.Fixtures.Api.Extensions;
using Sporeo.Fixtures.Application.Catalogs.Queries.GetCatalog;
using Sporeo.Fixtures.Contracts.Catalogs.Requests;
using Sporeo.Fixtures.Contracts.Catalogs.Responses;
using Sporeo.Fixtures.Contracts.Common;

namespace Sporeo.Fixtures.Api.Endpoints.Catalogs;

public sealed class CatalogEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/admin/catalogs")
            .WithTags("Catalogs");

        group.MapGet("/", GetCatalogAsync)
            .WithName("GetCatalog")
            .WithSummary("Gets a paged catalog of sports and leagues.")
            .WithDescription("Retrieves a cached, paginated list of sports and their associated leagues from the external provider." +
                "Used by administrators to browse available competitions.")
            .Produces<PagedResponse<CatalogSportResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            // .ProducesProblem(StatusCodes.Status401Unauthorized) // Uncomment if authentication is required
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> GetCatalogAsync(
        [AsParameters] GetCatalogRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var paginationParams = new PaginationParams(request.PageNumber, request.PageSize);
        var query = new GetCatalogQuery(paginationParams);

        var result = await sender.Send(query, cancellationToken);

        var response = result.Adapt<PagedResponse<CatalogSportResponse>>();

        return Results.Ok(response);
    }
}
