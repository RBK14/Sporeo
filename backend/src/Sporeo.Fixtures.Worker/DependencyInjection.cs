using Quartz;
using Sporeo.Fixtures.Worker.Configuration;
using Sporeo.Fixtures.Worker.Jobs;

namespace Sporeo.Fixtures.Worker;

/// <summary>
/// Registers Worker configuration and Quartz scheduling services.
/// </summary>
public static class DependencyInjection
{
    private const string SyncJobsGroup = "SyncJobs";
    private const string OutboxGroup = "Outbox";

    /// <summary>
    /// Binds and validates Worker configuration options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddWorkerConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SyncJobsRootOptions>()
            .Bind(configuration.GetSection(SyncJobsRootOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Registers Quartz with a persistent SQL Server store, clustering, outbox processing,
    /// and one durable sync job/trigger per configured <see cref="SyncJobOptions"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddWorkerServices(this IServiceCollection services, IConfiguration configuration)
    {
        var quartzConnectionString = configuration.GetConnectionString("quartz-db")
            ?? throw new InvalidOperationException("Connection string 'quartz-db' was not found.");

        var syncJobs = configuration
            .GetSection(SyncJobsRootOptions.SectionName)
            .Get<SyncJobsRootOptions>()
            ?? new SyncJobsRootOptions();

        services.AddQuartz(q =>
        {
            q.UseDefaultThreadPool(tp => { tp.MaxConcurrency = 10; });

            q.UsePersistentStore(store =>
            {
                store.UseSqlServer(quartzConnectionString);
                store.UseSystemTextJsonSerializer();
                store.UseClustering(c =>
                {
                    c.CheckinMisfireThreshold = TimeSpan.FromSeconds(20);
                    c.CheckinInterval = TimeSpan.FromSeconds(10);
                });
            });

            foreach (var jobOptions in syncJobs.Jobs)
            {
                var jobKey = new JobKey(jobOptions.JobId, SyncJobsGroup);

                q.AddJob<SyncFixturesJob>(opts => opts
                    .WithIdentity(jobKey)
                    .UsingJobData("SyncMode", jobOptions.SyncMode.ToString())
                    .UsingJobData("ProviderName", jobOptions.ProviderName)
                    .UsingJobData("ExternalLeagueId", jobOptions.ExternalLeagueId)
                    .UsingJobData("ExternalSeasonId", jobOptions.ExternalSeasonId ?? string.Empty)
                    .UsingJobData("SportId", jobOptions.SportId.ToString())
                    .UsingJobData("LeagueId", jobOptions.LeagueId?.ToString() ?? string.Empty)
                    .UsingJobData("SeasonId", jobOptions.SeasonId?.ToString() ?? string.Empty)
                    .StoreDurably());

                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity($"{jobOptions.JobId}-trigger", SyncJobsGroup)
                    .WithCronSchedule(jobOptions.CronSchedule));
            }

            var outboxJobKey = new JobKey(nameof(OutboxProcessorJob), OutboxGroup);
            q.AddJob<OutboxProcessorJob>(opts => opts.WithIdentity(outboxJobKey));

            q.AddTrigger(opts => opts
                .ForJob(outboxJobKey)
                .WithIdentity($"{nameof(OutboxProcessorJob)}-trigger", OutboxGroup)
                .WithSimpleSchedule(schedule => schedule
                    .WithInterval(TimeSpan.FromSeconds(10))
                    .RepeatForever()));
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
            options.AwaitApplicationStarted = true;
        });

        return services;
    }
}
