namespace Sporeo.BuildingBlocks.Application.Pagination;

public record PaginationParams(int Page, int PageSize)
{
    public int Offset => (Page - 1) * PageSize;
}
