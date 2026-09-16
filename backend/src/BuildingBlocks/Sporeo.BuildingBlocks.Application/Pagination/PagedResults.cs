namespace Sporeo.BuildingBlocks.Application.Pagination;

/// <summary>
/// Represents a single page of results together with total-count metadata.
/// </summary>
/// <typeparam name="T">The type of items contained in the page.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Gets the items included in the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Gets the total number of items matching the query before paging.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Gets a value indicating whether another page of results is available after the current page.
    /// </summary>
    public bool HasNextPage => Pagination.Offset + Items.Count < TotalCount;

    /// <summary>
    /// Gets the paging parameters used to produce this result.
    /// </summary>
    public PaginationParams Pagination { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResult{T}"/> class.
    /// </summary>
    /// <param name="items">The items included in the current page.</param>
    /// <param name="totalCount">The total number of items matching the query before paging.</param>
    /// <param name="pagination">The paging parameters used to produce this result.</param>
    public PagedResult(IEnumerable<T> items, int totalCount, PaginationParams pagination)
    {
        Items = items.ToList();
        TotalCount = totalCount;
        Pagination = pagination;
    }
}
