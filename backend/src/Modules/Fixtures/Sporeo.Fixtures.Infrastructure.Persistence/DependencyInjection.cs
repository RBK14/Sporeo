using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Application.Abstractions.Caching;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Abstractions;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Persistence;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Serialization;
using Sporeo.BuildingBlocks.Infrastructure.Persistence.Dapper;
using Sporeo.BuildingBlocks.Infrastructure.Persistence.Interceptors;
using Sporeo.Fixtures.Application.Fixtures.Data;
using Sporeo.Fixtures.Application.Leagues.Data;
using Sporeo.Fixtures.Application.Seasons.Data;
using Sporeo.Fixtures.Application.Sports.Data;
using Sporeo.Fixtures.Application.Venues.Data;
using Sporeo.Fixtures.Domain.Fixtures.ValueObjects;
using Sporeo.Fixtures.Domain.Leagues.Events;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;
using Sporeo.Fixtures.Domain.Venues.Events;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;
using Sporeo.Fixtures.Infrastructure.Persistence.Caching;
using Sporeo.Fixtures.Infrastructure.Persistence.Reading;
using Sporeo.Fixtures.Infrastructure.Persistence.Writing;
using Sporeo.Fixtures.Infrastructure.Persistence.Writing.Exceptions;
using Sporeo.Fixtures.Infrastructure.Persistence.Writing.Interceptors;
using Sporeo.Fixtures.Infrastructure.Persistence.Outbox;
using Sporeo.Fixtures.Infrastructure.Persistence.Reading.ReadStores;
using Sporeo.Fixtures.Infrastructure.Persistence.Writing.Repositories;
using Sporeo.Fixtures.Infrastructure.Persistence.Seeding;
using Sporeo.Fixtures.Application.Abstractions;

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
        TypedIdTypeHandlerRegistration.Register(FixtureId.FromValue);
        TypedIdTypeHandlerRegistration.Register(VenueId.FromValue);
        TypedIdTypeHandlerRegistration.Register(SportId.FromValue);
        TypedIdTypeHandlerRegistration.Register(LeagueId.FromValue);
        TypedIdTypeHandlerRegistration.Register(SeasonId.FromValue);

        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<VenueLocationInterceptor>();
        services.AddSingleton<IDomainEventTypeRegistry>(_ => new DomainEventTypeRegistry(
        [
            new KeyValuePair<string, Type>(
                FixturesOutboxTypeKeys.VenueCreatedDomainEvent,
                typeof(VenueCreatedDomainEvent)),

            new KeyValuePair<string, Type>(
                FixturesOutboxTypeKeys.LeagueMonitoringEnabledDomainEvent,
                typeof(LeagueMonitoringEnabledDomainEvent))

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
    /// <exception cref="InvalidOperationException">Thrown when the <c>redis</c> connection string is missing or blank.</exception>
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("redis");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'redis' was not found.");
        }

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
        var connectionString = configuration.GetConnectionString("fixtures-db");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'fixtures-db' was not found.");
        }

        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connectionString));

        services.AddDbContext<FixturesDbContext>((sp, options) =>
        {
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<VenueLocationInterceptor>());

            options.ConfigureFixturesSqlServer(connectionString);
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
        services.AddScoped<ISportReadStore, SportReadStore>();

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
