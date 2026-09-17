using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Application.Abstractions.Caching;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Abstractions;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Persistence;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Serialization;
using Sporeo.BuildingBlocks.Infrastructure.Persistence.Interceptors;
using Sporeo.Fixtures.Application.Abstractions.Persistence;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.ReadModels;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.Repositories;
using Sporeo.Fixtures.Application.Leagues.Abstractions.ReadModels;
using Sporeo.Fixtures.Application.Leagues.Abstractions.Repositories;
using Sporeo.Fixtures.Application.Seasons.Abstractions.Repositories;
using Sporeo.Fixtures.Application.Sports.Abstractions.Repositories;
using Sporeo.Fixtures.Application.Venues.Abstractions.ReadModels;
using Sporeo.Fixtures.Application.Venues.Abstractions.Repositories;
using Sporeo.Fixtures.Domain.Venues.Events;
using Sporeo.Fixtures.Infrastructure.Persistence.Caching;
using Sporeo.Fixtures.Infrastructure.Persistence.Connections;
using Sporeo.Fixtures.Infrastructure.Persistence.Context;
using Sporeo.Fixtures.Infrastructure.Persistence.Exceptions;
using Sporeo.Fixtures.Infrastructure.Persistence.Interceptors;
using Sporeo.Fixtures.Infrastructure.Persistence.Logging;
using Sporeo.Fixtures.Infrastructure.Persistence.Outbox;
using Sporeo.Fixtures.Infrastructure.Persistence.ReadModels;
using Sporeo.Fixtures.Infrastructure.Persistence.Repositories;
using Sporeo.Fixtures.Infrastructure.Persistence.Seeding;

namespace Sporeo.Fixtures.Infrastructure.Persistence;

/// <summary>
/// Registers Fixtures persistence services with the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the minimal Fixtures database stack required for migrations and seeding
    /// (EF Core context, interceptors, domain-event type registry, and seeder).
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration containing the connection string.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddFixturesDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<VenueLocationInterceptor>();
        services.AddSingleton<IDomainEventTypeRegistry>(_ => new DomainEventTypeRegistry(
        [
            new KeyValuePair<string, Type>(
                FixturesOutboxTypeKeys.VenueCreatedDomainEvent,
                typeof(VenueCreatedDomainEvent))
        ]));
        services.AddScoped<FixturesDatabaseSeeder>();
        services.AddSqlServer(configuration);

        return services;
    }

    /// <summary>
    /// Adds the full Fixtures EF Core persistence layer, including repositories and outbox storage.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration containing the connection string.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFixturesDatabase(configuration);
        services.AddSingleton<IDatabaseExceptionClassifier, SqlServerDatabaseExceptionClassifier>();
        services.AddScoped<IOutboxStore, EfOutboxStore<FixturesDbContext>>();
        services.AddRepositories();

        return services;
    }

    /// <summary>
    /// Adds Redis-backed distributed caching for the Fixtures module.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration containing the <c>redis</c> connection string.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the <c>redis</c> connection string is missing.</exception>
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("redis")
            ?? throw new InvalidOperationException("Connection string 'redis' was not found.");

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = "fixtures-cache_";
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }

    private static IServiceCollection AddSqlServer(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("fixtures-db")
            ?? throw new InvalidOperationException("Connection string 'fixtures-db' was not found.");

        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connectionString));

        services.AddDbContext<FixturesDbContext>((sp, options) =>
        {
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<VenueLocationInterceptor>());

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.UseNetTopologySuite();
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");

                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            });
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<FixturesDbContext>());

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IFixtureRepository, FixtureRepository>();
        services.AddScoped<IVenueRepository, VenueRepository>();
        services.AddScoped<ILeagueRepository, LeagueRepository>();
        services.AddScoped<ISeasonRepository, SeasonRepository>();
        services.AddScoped<ISportRepository, SportRepository>();
        services.AddScoped<IFixtureReadStore, FixtureReadStore>();
        services.AddScoped<IVenueReadStore, VenueReadStore>();
        services.AddScoped<ILeagueReadStore, LeagueReadStore>();

        return services;
    }

    /// <summary>
    /// Applies pending EF Core migrations for the Fixtures database.
    /// </summary>
    /// <param name="serviceProvider">The root service provider.</param>
    /// <returns>A task that completes when migrations have been applied.</returns>
    public static async Task InitializeFixturesDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var dbContext = services.GetRequiredService<FixturesDbContext>();
            await dbContext.Database.MigrateAsync();

            var seeder = services.GetRequiredService<FixturesDatabaseSeeder>();
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitialization");
            logger.LogDatabaseMigrationError(ex);
            throw;
        }
    }
}
