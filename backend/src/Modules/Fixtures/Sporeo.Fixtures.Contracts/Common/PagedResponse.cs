namespace Sporeo.Fixtures.Contracts.Common;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    bool HasNextPage,
    int PageNumber,
    int PageSize);
