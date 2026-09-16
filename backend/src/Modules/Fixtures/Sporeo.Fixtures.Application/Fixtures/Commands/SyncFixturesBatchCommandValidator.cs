using FluentValidation;
using Sporeo.Fixtures.Application.Abstractions.Providers;

namespace Sporeo.Fixtures.Application.Fixtures.Commands;

/// <summary>
/// Validates <see cref="SyncFixturesBatchCommand"/> contracts before handling.
/// </summary>
internal sealed class SyncFixturesBatchCommandValidator : AbstractValidator<SyncFixturesBatchCommand>
{
    public SyncFixturesBatchCommandValidator()
    {
        RuleFor(command => command.SportId)
            .NotEmpty()
            .WithErrorCode("SyncFixtures.InvalidSportId");

        RuleFor(command => command.LeagueId)
            .Must(id => id is null || id != Guid.Empty)
            .WithErrorCode("SyncFixtures.InvalidLeagueId");

        RuleFor(command => command.SeasonId)
            .Must(id => id is null || id != Guid.Empty)
            .WithErrorCode("SyncFixtures.InvalidSeasonId");

        RuleFor(command => command.ProviderName)
            .NotEmpty()
            .WithErrorCode("SyncFixtures.InvalidProviderName");

        RuleFor(command => command.Fixtures)
            .NotNull()
            .WithErrorCode("SyncFixtures.FixturesRequired")
            .Must(fixtures => fixtures.Count > 0)
            .WithErrorCode("SyncFixtures.FixturesEmpty")
            .Must(fixtures => fixtures.Count <= SyncFixturesBatchCommand.MaxBatchSize)
            .WithErrorCode("SyncFixtures.BatchTooLarge")
            .WithMessage($"Batch size must be between 1 and {SyncFixturesBatchCommand.MaxBatchSize}.");

        RuleFor(command => command.Fixtures)
            .Must(HaveUniqueProviderIds)
            .WithErrorCode("SyncFixtures.DuplicateProviderIds")
            .WithMessage("Fixture provider identifiers must be unique within a batch.");

        RuleFor(command => command)
            .Must(HaveHomogeneousProvider)
            .WithErrorCode("SyncFixtures.MixedProviders")
            .WithMessage("All fixtures in a batch must use the command ProviderName.");

        RuleForEach(command => command.Fixtures).ChildRules(fixture =>
        {
            fixture.RuleFor(item => item.ProviderId)
                .NotEmpty()
                .WithErrorCode("SyncFixtures.EmptyFixtureProviderId");

            fixture.RuleFor(item => item.ProviderName)
                .NotEmpty()
                .WithErrorCode("SyncFixtures.EmptyFixtureProviderName");

            fixture.RuleFor(item => item.Name)
                .NotEmpty()
                .WithErrorCode("SyncFixtures.EmptyFixtureName");

            fixture.When(item => item.Venue is not null, () =>
            {
                fixture.RuleFor(item => item.Venue!.ProviderId)
                    .NotEmpty()
                    .WithErrorCode("SyncFixtures.EmptyVenueProviderId");

                fixture.RuleFor(item => item.Venue!.Name)
                    .NotEmpty()
                    .WithErrorCode("SyncFixtures.EmptyVenueName");
            });
        });
    }

    private static bool HaveUniqueProviderIds(IReadOnlyList<ExternalFixtureDto> fixtures) =>
        fixtures.Select(fixture => fixture.ProviderId).Distinct(StringComparer.Ordinal).Count() == fixtures.Count;

    private static bool HaveHomogeneousProvider(SyncFixturesBatchCommand command) =>
        command.Fixtures.All(fixture =>
            string.Equals(fixture.ProviderName, command.ProviderName, StringComparison.Ordinal) &&
            (fixture.Venue is null ||
             string.Equals(fixture.Venue.ProviderName, command.ProviderName, StringComparison.Ordinal)));
}
