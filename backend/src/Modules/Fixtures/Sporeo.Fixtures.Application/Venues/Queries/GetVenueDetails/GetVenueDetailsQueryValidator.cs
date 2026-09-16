using FluentValidation;

namespace Sporeo.Fixtures.Application.Venues.Queries.GetVenueDetails;

/// <summary>
/// Validates <see cref="GetVenueDetailsQuery"/> parameters.
/// </summary>
internal sealed class GetVenueDetailsQueryValidator : AbstractValidator<GetVenueDetailsQuery>
{
    public GetVenueDetailsQueryValidator()
    {
        RuleFor(query => query.VenueId)
            .NotEmpty()
            .WithErrorCode("Venue.InvalidId")
            .WithMessage("Venue ID cannot be empty.");
    }
}
