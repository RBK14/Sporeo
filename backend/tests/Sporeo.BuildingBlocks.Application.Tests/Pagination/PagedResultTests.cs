using FluentAssertions;
using Sporeo.BuildingBlocks.Application.Pagination;

namespace Sporeo.BuildingBlocks.Application.Tests.Pagination;

public class PagedResultTests
{
    [Fact]
    public void HasNextPage_OnFirstPageWithMoreItems_ShouldBeTrue()
    {
        var pagination = new PaginationParams(1, 10);
        var result = new PagedResult<int>(Enumerable.Range(1, 10), 25, pagination);

        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_OnMiddlePage_ShouldBeTrue()
    {
        var pagination = new PaginationParams(2, 10);
        var result = new PagedResult<int>(Enumerable.Range(11, 10), 25, pagination);

        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_OnLastPartialPage_ShouldBeFalse()
    {
        var pagination = new PaginationParams(3, 10);
        var result = new PagedResult<int>(Enumerable.Range(21, 5), 25, pagination);

        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasNextPage_WhenEmpty_ShouldBeFalse()
    {
        var pagination = new PaginationParams(1, 10);
        var result = new PagedResult<int>([], 0, pagination);

        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasNextPage_WhenExactLastPage_ShouldBeFalse()
    {
        var pagination = new PaginationParams(2, 10);
        var result = new PagedResult<int>(Enumerable.Range(11, 10), 20, pagination);

        result.HasNextPage.Should().BeFalse();
    }
}
