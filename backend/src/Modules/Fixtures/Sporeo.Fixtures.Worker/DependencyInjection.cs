using Microsoft.Extensions.Options;
using Quartz;
using Sporeo.Fixtures.Worker.Configuration;
using Sporeo.Fixtures.Worker.Jobs.Fixtures;
using Sporeo.Fixtures.Worker.Jobs.Outbox;

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
        services.AddSingleton<IValidateOptions<WorkerOptions>, WorkerOptionsValidator>();

        services.AddOptions<WorkerOptions>()
            .Bind(configuration.GetSection(WorkerOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Registers Quartz with a persistent SQL Server store, clustering, outbox processing,
    /// and one durable sync job/trigger per configured <see cref="WorkerOptions"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddWorkerServices(this IServiceCollection services, IConfiguration configuration)
    {
        var quartzConnectionString = configuration.GetConnectionString("quartz-db")
            ?? throw new InvalidOperationException("Connection string 'quartz-db' was not found.");

        var workerOptions = configuration.GetSection(WorkerOptions.SectionName).Get<WorkerOptions>()
                            ?? new WorkerOptions();

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

            var dispatcherJobKey = new JobKey(nameof(SyncDispatcherJob), SyncJobsGroup);
            q.AddJob<SyncDispatcherJob>(opts => opts.WithIdentity(dispatcherJobKey).StoreDurably());
            q.AddTrigger(opts => opts
                .ForJob(dispatcherJobKey)
                .WithIdentity($"{nameof(SyncDispatcherJob)}-trigger", SyncJobsGroup)
                .WithCronSchedule(workerOptions.DispatcherCron));

            var shortTermJobKey = new JobKey(nameof(ShortTermSyncJob), SyncJobsGroup);
            q.AddJob<ShortTermSyncJob>(opts => opts.WithIdentity(shortTermJobKey).StoreDurably());

            var outboxJobKey = new JobKey(nameof(OutboxProcessorJob), OutboxGroup);
            q.AddJob<OutboxProcessorJob>(opts => opts.WithIdentity(outboxJobKey));
            q.AddTrigger(opts => opts
                .ForJob(outboxJobKey)
                .WithIdentity($"{nameof(OutboxProcessorJob)}-trigger", OutboxGroup)
                .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromSeconds(10)).RepeatForever()));
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
            options.AwaitApplicationStarted = true;
        });

        return services;
    }
}
