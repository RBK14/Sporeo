using FluentAssertions;
using Sporeo.Fixtures.Worker.Configuration;

namespace Sporeo.Fixtures.Worker.Tests.Configuration;

public sealed class WorkerOptionsTests
{
    private readonly WorkerOptionsValidator _validator = new();

    [Fact]
    public void Validate_WithValidCrons_ShouldSucceed()
    {
        var result = _validator.Validate(null, CreateValidOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyDispatcherCron_ShouldFail()
    {
        var options = new WorkerOptions
        {
            DispatcherCron = " ",
            LongTermSyncCron = "0 0 3 * * ?",
            SeasonsSyncCron = "0 0 2 ? * MON"
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains(nameof(WorkerOptions.DispatcherCron)));
    }

    [Fact]
    public void Validate_WithInvalidLongTermSyncCron_ShouldFail()
    {
        var options = new WorkerOptions
        {
            DispatcherCron = "0 30 * * * ?",
            LongTermSyncCron = "not-a-cron",
            SeasonsSyncCron = "0 0 2 ? * MON"
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains(nameof(WorkerOptions.LongTermSyncCron)));
    }

    [Fact]
    public void Validate_WithInvalidSeasonsSyncCron_ShouldFail()
    {
        var options = new WorkerOptions
        {
            DispatcherCron = "0 30 * * * ?",
            LongTermSyncCron = "0 0 3 * * ?",
            SeasonsSyncCron = "0 0 2"
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains(nameof(WorkerOptions.SeasonsSyncCron)));
    }

    private static WorkerOptions CreateValidOptions() =>
        new()
        {
            DispatcherCron = "0 30 * * * ?",
            LongTermSyncCron = "0 0 3 * * ?",
            SeasonsSyncCron = "0 0 2 ? * MON"
        };
}
