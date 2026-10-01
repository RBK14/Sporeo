using FluentValidation;
using Sporeo.Fixtures.Application.Common;
using DomainErrors = Sporeo.Fixtures.Domain.Common.Errors;

namespace Sporeo.Fixtures.Application.Leagues.Commands.CreateLeague;

internal sealed class CreateLeagueCommandValidator : AbstractValidator<CreateLeagueCommand>
{
    public CreateLeagueCommandValidator()
    {
        RuleFor(command => command.SportId)
            .NotNull()
            .WithErrorCode(Errors.League.SportIdRequired.Code)
            .WithMessage(Errors.League.SportIdRequired.Message);

        RuleFor(command => command.Name)
            .NotEmpty()
            .WithErrorCode(DomainErrors.League.EmptyName.Code)
            .WithMessage(DomainErrors.League.EmptyName.Message);
    }
}
