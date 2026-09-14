using FluentValidation;
using Sporeo.Fixtures.Application.Common.Validation;

namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtures;

/// <summary>
/// Validates <see cref="GetFixturesQuery"/> paging and filter parameters.
/// </summary>
internal sealed class GetFixturesQueryValidator : AbstractValidator<GetFixturesQuery>
{
    public GetFixturesQueryValidator()
    {
        RuleFor(query => query.Pagination)
            .NotNull()
            .WithErrorCode("Pagination.Required")
            .WithMessage("Pagination is required.");

        RuleFor(query => query.Filters)
            .NotNull()
            .WithErrorCode("FixtureFilters.Required")
            .WithMessage("Filters are required.");

        FixtureQueryValidationRules.ApplyPaginationRules(this, query => query.Pagination);
        FixtureQueryValidationRules.ApplyFixtureFilterRules(this, query => query.Filters);
    }
}
