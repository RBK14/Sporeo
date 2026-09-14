namespace Sporeo.BuildingBlocks.Application.Pagination;

/// <summary>
/// Represents paging parameters for a query that returns a page of results.
/// </summary>
/// <param name="Page">The 1-based page number to retrieve.</param>
/// <param name="PageSize">The maximum number of items to include on a single page.</param>
public record PaginationParams(int Page, int PageSize)
{
    /// <summary>
    /// Gets the zero-based offset of the first item for the requested page.
    /// </summary>
    public long Offset => (long)(Page - 1) * PageSize;
}
