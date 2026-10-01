using FluentValidation;
using Sporeo.Fixtures.Application.Common;
using DomainErrors = Sporeo.Fixtures.Domain.Common.Errors;

namespace Sporeo.Fixtures.Application.Seasons.Commands.CreateSeason;

internal sealed class CreateSeasonCommandValidator : AbstractValidator<CreateSeasonCommand>
{
    public CreateSeasonCommandValidator()
    {
        RuleFor(command => command.LeagueId)
            .NotNull()
            .WithErrorCode(Errors.Season.LeagueIdRequired.Code)
            .WithMessage(Errors.Season.LeagueIdRequired.Message);

        RuleFor(command => command.Name)
            .NotEmpty()
            .WithErrorCode(DomainErrors.Season.EmptyName.Code)
            .WithMessage(DomainErrors.Season.EmptyName.Message);
    }
}
