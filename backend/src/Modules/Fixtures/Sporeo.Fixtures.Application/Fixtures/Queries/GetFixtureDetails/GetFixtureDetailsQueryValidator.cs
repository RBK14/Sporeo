using FluentValidation;

namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtureDetails;

/// <summary>
/// Validates <see cref="GetFixtureDetailsQuery"/> parameters.
/// </summary>
internal sealed class GetFixtureDetailsQueryValidator : AbstractValidator<GetFixtureDetailsQuery>
{
    public GetFixtureDetailsQueryValidator()
    {
        RuleFor(query => query.FixtureId)
            .NotEmpty()
            .WithErrorCode("Fixture.InvalidId")
            .WithMessage("Fixture ID cannot be empty.");
    }
}
