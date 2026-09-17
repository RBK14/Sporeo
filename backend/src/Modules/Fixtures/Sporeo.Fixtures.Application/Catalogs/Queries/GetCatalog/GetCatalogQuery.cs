using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Application.Pagination;

namespace Sporeo.Fixtures.Application.Catalogs.Queries.GetCatalog;

/// <summary>
/// Query that retrieves a paged catalog of sports and leagues from the external provider (cached).
/// </summary>
/// <param name="Pagination">The page number and page size used to slice the sports list.</param>
public sealed record GetCatalogQuery(PaginationParams Pagination) : IQuery<PagedResult<CatalogSportResponse>>;
