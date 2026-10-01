using FluentValidation;
using Sporeo.Fixtures.Application.Common.Validation;

namespace Sporeo.Fixtures.Application.Catalogs.Queries.GetCatalog;

/// <summary>
/// Validates <see cref="GetCatalogQuery"/> paging parameters.
/// </summary>
internal sealed class GetCatalogQueryValidator : AbstractValidator<GetCatalogQuery>
{
    public GetCatalogQueryValidator()
    {
        RuleFor(query => query.Pagination)
            .NotNull()
            .WithErrorCode("Pagination.Required")
            .WithMessage("Pagination is required.");

        PagedQueryValidationRules.ApplyPaginationRules(this, query => query.Pagination);
    }
}
