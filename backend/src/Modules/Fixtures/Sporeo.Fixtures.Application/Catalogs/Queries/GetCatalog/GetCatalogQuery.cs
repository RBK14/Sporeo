using MediatR;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Application.Pagination;

namespace Sporeo.Fixtures.Application.Catalogs.Queries.GetCatalog;

public sealed record GetCatalogQuery(PaginationParams Pagination) : IQuery<PagedResult<CatalogSportResponse>>; 
