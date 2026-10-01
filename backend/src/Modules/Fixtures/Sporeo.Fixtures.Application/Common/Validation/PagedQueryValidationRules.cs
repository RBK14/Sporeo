using FluentValidation;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.Fixtures.Application.Fixtures.Queries.Common;

namespace Sporeo.Fixtures.Application.Common.Validation;

/// <summary>
/// Shared FluentValidation rules for fixture query contracts.
/// </summary>
internal static class PagedQueryValidationRules
{
    public const int MaxPageSize = 100;

    public static void ApplyPaginationRules<T>(
        AbstractValidator<T> validator,
        Func<T, PaginationParams> paginationSelector)
    {
        validator.RuleFor(query => paginationSelector(query).PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode("Pagination.InvalidPageNumber")
            .WithMessage("PageNumber must be greater than or equal to 1.");

        validator.RuleFor(query => paginationSelector(query).PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .WithErrorCode("Pagination.InvalidPageSize")
            .WithMessage($"Page size must be between 1 and {MaxPageSize}.");
    }

    public static void ApplyFixtureFilterRules<T>(
        AbstractValidator<T> validator,
        Func<T, FixtureFilters> filtersSelector)
    {
        validator.RuleFor(query => filtersSelector(query))
            .Must(filters =>
                filters.DateFrom is null
                || filters.DateTo is null
                || filters.DateFrom <= filters.DateTo)
            .WithErrorCode("FixtureFilters.InvalidDateRange")
            .WithMessage("DateFrom must be less than or equal to DateTo.");
    }
}
