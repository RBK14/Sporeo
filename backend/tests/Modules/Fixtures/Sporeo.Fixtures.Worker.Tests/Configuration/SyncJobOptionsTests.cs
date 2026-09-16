using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Sporeo.Fixtures.Application.Abstractions.Providers;
using Sporeo.Fixtures.Worker.Configuration;

namespace Sporeo.Fixtures.Worker.Tests.Configuration;

public sealed class SyncJobOptionsTests
{
    [Fact]
    public void Validate_WithValidShortTermJob_ShouldSucceed()
    {
        var results = Validate(CreateValidJob());

        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithInvalidCron_ShouldFail()
    {
        var options = CreateValidJob(cronSchedule: "not-a-cron");

        var results = Validate(options);

        results.Should().Contain(result => result.MemberNames.Contains(nameof(SyncJobOptions.CronSchedule)));
    }

    [Fact]
    public void Validate_WithLongTermMissingSeason_ShouldFail()
    {
        var options = CreateValidJob(syncMode: SyncMode.LongTerm, externalSeasonId: null);

        var results = Validate(options);

        results.Should().Contain(result => result.MemberNames.Contains(nameof(SyncJobOptions.ExternalSeasonId)));
    }

    [Fact]
    public void Validate_WithEmptySportId_ShouldFail()
    {
        var options = CreateValidJob(sportId: Guid.Empty);

        var results = Validate(options);

        results.Should().Contain(result => result.MemberNames.Contains(nameof(SyncJobOptions.SportId)));
    }

    [Fact]
    public void RootOptions_WithDuplicateJobIds_ShouldFail()
    {
        var root = new SyncJobsRootOptions
        {
            Jobs =
            [
                CreateValidJob(jobId: "premier-league"),
                CreateValidJob(jobId: "Premier-League", externalLeagueId: "9999")
            ]
        };

        var results = Validate(root);

        results.Should().Contain(result => result.MemberNames.Contains(nameof(SyncJobsRootOptions.Jobs)));
    }

    private static SyncJobOptions CreateValidJob(
        string jobId = "premier-league-short",
        string cronSchedule = "0 0/15 * * * ?",
        SyncMode syncMode = SyncMode.ShortTerm,
        string? externalSeasonId = null,
        string externalLeagueId = "4328",
        Guid? sportId = null) =>
        new()
        {
            JobId = jobId,
            CronSchedule = cronSchedule,
            SyncMode = syncMode,
            ProviderName = "TheSportsDB",
            ExternalLeagueId = externalLeagueId,
            ExternalSeasonId = externalSeasonId,
            SportId = sportId ?? Guid.Parse("11111111-1111-1111-1111-111111111111")
        };

    private static List<ValidationResult> Validate(object instance)
    {
        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        return results;
    }
}
