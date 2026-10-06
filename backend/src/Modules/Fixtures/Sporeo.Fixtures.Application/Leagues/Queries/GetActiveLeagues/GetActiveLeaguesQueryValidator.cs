using FluentValidation;
using Sporeo.Fixtures.Application.Common;

namespace Sporeo.Fixtures.Application.Leagues.Queries.GetActiveLeagues;

/// <summary>
/// Validates <see cref="GetActiveLeaguesQuery"/> filter parameters.
/// </summary>
internal sealed class GetActiveLeaguesQueryValidator : AbstractValidator<GetActiveLeaguesQuery>
{
    public GetActiveLeaguesQueryValidator()
    {
        RuleFor(query => query.SportId!.Value)
            .NotEqual(Guid.Empty)
            .WithErrorCode(Errors.League.InvalidSportId.Code)
            .WithMessage(Errors.League.InvalidSportId.Message)
            .When(query => query.SportId is not null);
    }
}
