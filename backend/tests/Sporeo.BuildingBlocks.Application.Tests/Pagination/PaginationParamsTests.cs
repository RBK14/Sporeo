using FluentAssertions;
using Sporeo.BuildingBlocks.Application.Pagination;

namespace Sporeo.BuildingBlocks.Application.Tests.Pagination;

public class PaginationParamsTests
{
    [Theory]
    [InlineData(1, 10, 0L)]
    [InlineData(3, 20, 40L)]
    [InlineData(2, 100, 100L)]
    public void Offset_ShouldUseZeroBasedPageCalculation(int page, int pageSize, long expectedOffset)
    {
        var pagination = new PaginationParams(page, pageSize);

        pagination.Offset.Should().Be(expectedOffset);
    }

    [Fact]
    public void Offset_ShouldAvoidIntegerOverflowForLargePageValues()
    {
        var pagination = new PaginationParams(int.MaxValue, 2);

        pagination.Offset.Should().Be((long)(int.MaxValue - 1) * 2);
    }
}
