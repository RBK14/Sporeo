using FluentAssertions;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.Fixtures.Application.Catalogs.Queries.GetCatalog;

namespace Sporeo.Fixtures.Application.Tests.Catalogs.Queries.GetCatalog;

public class GetCatalogQueryValidatorTests
{
    private readonly GetCatalogQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidPagination_ShouldSucceed()
    {
        var query = new GetCatalogQuery(new PaginationParams(1, 10));

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 10, "Pagination.InvalidPageNumber")]
    [InlineData(1, 0, "Pagination.InvalidPageSize")]
    [InlineData(1, 101, "Pagination.InvalidPageSize")]
    public void Validate_WithInvalidPagination_ShouldFail(int pageNumber, int pageSize, string expectedErrorCode)
    {
        var query = new GetCatalogQuery(new PaginationParams(pageNumber, pageSize));

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorCode == expectedErrorCode);
    }
}
