namespace Sporeo.BuildingBlocks.Application.Pagination;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public bool HasNextPage => TotalCount > Items.Count;
    public PaginationParams Pagination { get; }

    public PagedResult(IEnumerable<T> items, int totalCount, PaginationParams pagination)
    {
        Items = items.ToList();
        TotalCount = totalCount;
        Pagination = pagination;
    }
}
