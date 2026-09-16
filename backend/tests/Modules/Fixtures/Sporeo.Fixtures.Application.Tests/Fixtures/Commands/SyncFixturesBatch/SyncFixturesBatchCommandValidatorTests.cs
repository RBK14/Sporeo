using FluentAssertions;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.Providers;
using Sporeo.Fixtures.Application.Fixtures.Commands.SyncFixturesBatch;
using Sporeo.Fixtures.Domain.Fixtures.Enums;

namespace Sporeo.Fixtures.Application.Tests.Fixtures.Commands;

public sealed class SyncFixturesBatchCommandValidatorTests
{
    private readonly SyncFixturesBatchCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidBatch_ShouldSucceed()
    {
        var command = CreateValidCommand();

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithDuplicateProviderIds_ShouldSucceed_ItemHandledByHandler()
    {
        var fixture = CreateFixture("1");
        var command = CreateValidCommand([fixture, fixture with { Name = "Other" }]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithMixedProviders_ShouldSucceed_ItemHandledByHandler()
    {
        var command = CreateValidCommand(
        [
            CreateFixture("1"),
            CreateFixture("2") with { ProviderName = "OtherProvider" }
        ]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyBatch_ShouldFail()
    {
        var command = CreateValidCommand([]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorCode == "SyncFixtures.FixturesEmpty");
    }

    [Fact]
    public async Task Validate_WithEmptySeasonId_ShouldFail()
    {
        var command = CreateValidCommand() with { SeasonId = Guid.Empty };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorCode == "SyncFixtures.InvalidSeasonId");
    }

    [Fact]
    public async Task Validate_WithBatchTooLarge_ShouldFail()
    {
        var fixtures = Enumerable.Range(1, SyncFixturesBatchCommand.MaxBatchSize + 1)
            .Select(index => CreateFixture(index.ToString()))
            .ToList();

        var result = await _validator.ValidateAsync(CreateValidCommand(fixtures));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorCode == "SyncFixtures.BatchTooLarge");
    }

    [Fact]
    public async Task Validate_WithEmptyProviderName_ShouldFail()
    {
        var command = CreateValidCommand() with { ProviderName = " " };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorCode == "SyncFixtures.InvalidProviderName");
    }

    private static SyncFixturesBatchCommand CreateValidCommand(
        IReadOnlyList<ExternalFixtureDto>? fixtures = null) =>
        new(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            null,
            "TheSportsDB",
            fixtures ?? [CreateFixture("1")]);

    private static ExternalFixtureDto CreateFixture(string providerId) =>
        new(
            providerId,
            "TheSportsDB",
            "Home vs Away",
            DateTimeOffset.UtcNow,
            FixtureStatus.Scheduled,
            null);
}
