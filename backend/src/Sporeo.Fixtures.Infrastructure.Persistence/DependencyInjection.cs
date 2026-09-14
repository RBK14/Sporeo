using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.BuildingBlocks.Infrastructure.Persistence;
using Sporeo.Fixtures.Domain.Fixtures;
using Sporeo.Fixtures.Domain.Leagues;
using Sporeo.Fixtures.Domain.Seasons;
using Sporeo.Fixtures.Domain.Sports;
using Sporeo.Fixtures.Domain.Venues;
using Sporeo.Fixtures.Infrastructure.Persistence.Connections;
using Sporeo.Fixtures.Infrastructure.Persistence.Contexts;
using Sporeo.Fixtures.Infrastructure.Persistence.Interceptors;
using Sporeo.Fixtures.Infrastructure.Persistence.Logging;
using Sporeo.Fixtures.Infrastructure.Persistence.Repositories;

namespace Sporeo.Fixtures.Infrastructure.Persistence;

/// <summary>
/// Registers Fixtures persistence services with the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the Fixtures EF Core persistence layer to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration containing the connection string.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<VenueLocationInterceptor>();
        services.AddSqlServer(configuration);
        services.AddRepositories();

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

            // todo: add database seeder
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitialization");
            logger.LogDatabaseMigrationError(ex);
            throw;
        }
    }
}
