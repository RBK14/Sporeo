namespace Sporeo.Fixtures.Contracts.Common;

/// <summary>
/// Standard paged payload returned by list endpoints.
/// </summary>
/// <typeparam name="T">Item type contained in the page.</typeparam>
/// <param name="Items">Items for the current page.</param>
/// <param name="TotalCount">Total number of matching items across all pages.</param>
/// <param name="HasNextPage">Indicates whether another page is available after the current one.</param>
/// <param name="PageNumber">1-based page index that was requested.</param>
/// <param name="PageSize">Requested page size.</param>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    bool HasNextPage,
    int PageNumber,
    int PageSize);
