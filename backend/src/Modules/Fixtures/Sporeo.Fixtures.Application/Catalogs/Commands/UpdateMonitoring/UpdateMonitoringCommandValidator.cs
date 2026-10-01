using FluentValidation;

namespace Sporeo.Fixtures.Application.Catalogs.Commands.UpdateMonitoring;

internal sealed class UpdateMonitoringCommandValidator : AbstractValidator<UpdateMonitoringCommand>
{
    public UpdateMonitoringCommandValidator()
    {
        RuleFor(command => command.Sports)
            .NotEmpty()
            .WithErrorCode("Catalog.InvalidRequest")
            .WithMessage("At least one sport must be provided.");

        RuleForEach(command => command.Sports).ChildRules(sport =>
        {
            sport.RuleFor(s => s.ProviderId)
                .NotEmpty()
                .WithErrorCode("Catalog.InvalidRequest")
                .WithMessage("Sport provider id is required.");

            sport.RuleFor(s => s.ProviderName)
                .NotEmpty()
                .WithErrorCode("Catalog.InvalidRequest")
                .WithMessage("Sport provider name is required.");

            sport.RuleForEach(s => s.Leagues).ChildRules(league =>
            {
                league.RuleFor(l => l.ProviderId)
                    .NotEmpty()
                    .WithErrorCode("Catalog.InvalidRequest")
                    .WithMessage("League provider id is required.");

                league.RuleFor(l => l.ProviderName)
                    .NotEmpty()
                    .WithErrorCode("Catalog.InvalidRequest")
                    .WithMessage("League provider name is required.");
            });
        });
    }
}
