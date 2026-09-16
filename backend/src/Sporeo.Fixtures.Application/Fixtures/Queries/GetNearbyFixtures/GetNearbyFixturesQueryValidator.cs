using FluentValidation;
using Sporeo.Fixtures.Application.Common.Validation;
using Sporeo.Fixtures.Domain.Common;

namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetNearbyFixtures;

/// <summary>
/// Validates <see cref="GetNearbyFixturesQuery"/> location, paging, and filter parameters.
/// </summary>
internal sealed class GetNearbyFixturesQueryValidator : AbstractValidator<GetNearbyFixturesQuery>
{
    public GetNearbyFixturesQueryValidator()
    {
        RuleFor(query => query.Latitude)
            .InclusiveBetween(-90, 90)
            .WithErrorCode(Errors.Venue.Coordinates.InvalidLatitude.Code)
            .WithMessage(Errors.Venue.Coordinates.InvalidLatitude.Message);

        RuleFor(query => query.Longitude)
            .InclusiveBetween(-180, 180)
            .WithErrorCode(Errors.Venue.Coordinates.InvalidLongitude.Code)
            .WithMessage(Errors.Venue.Coordinates.InvalidLongitude.Message);

        RuleFor(query => query.RadiusInMeters)
            .GreaterThan(0)
            .WithErrorCode("NearbyFixtures.InvalidRadius")
            .WithMessage("Radius must be greater than zero.");

        RuleFor(query => query.Pagination)
            .NotNull()
            .WithErrorCode("Pagination.Required")
            .WithMessage("Pagination is required.");

        RuleFor(query => query.Filters)
            .NotNull()
            .WithErrorCode("FixtureFilters.Required")
            .WithMessage("Filters are required.");

        PagedQueryValidationRules.ApplyPaginationRules(this, query => query.Pagination);
        PagedQueryValidationRules.ApplyFixtureFilterRules(this, query => query.Filters);
    }
}
