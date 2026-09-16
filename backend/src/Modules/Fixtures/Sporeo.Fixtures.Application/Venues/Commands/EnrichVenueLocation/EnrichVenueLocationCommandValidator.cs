using FluentValidation;

namespace Sporeo.Fixtures.Application.Venues.Commands.EnrichVenueLocation;

/// <summary>
/// Validates <see cref="EnrichVenueLocationCommand"/> parameters.
/// </summary>
internal sealed class EnrichVenueLocationCommandValidator : AbstractValidator<EnrichVenueLocationCommand>
{
    public EnrichVenueLocationCommandValidator()
    {
        RuleFor(command => command.VenueId)
            .NotEmpty()
            .WithErrorCode("Venue.InvalidId")
            .WithMessage("Venue ID cannot be empty.");
    }
}
