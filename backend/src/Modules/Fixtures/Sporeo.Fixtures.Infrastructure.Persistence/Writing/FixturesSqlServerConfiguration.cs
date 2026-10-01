using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Writing;

/// <summary>
/// Shared SQL Server configuration for runtime DI and EF Core design-time tooling.
/// </summary>
internal static class FixturesSqlServerConfiguration
{
    /// <summary>
    /// Applies the Fixtures SQL Server provider options (NetTopologySuite, migrations history, retries).
    /// </summary>
    public static DbContextOptionsBuilder<FixturesDbContext> ConfigureFixturesSqlServer(
        this DbContextOptionsBuilder<FixturesDbContext> optionsBuilder,
        string connectionString)
    {
        optionsBuilder.UseSqlServer(connectionString, ConfigureSqlServer);
        return optionsBuilder;
    }

    /// <summary>
    /// Applies the Fixtures SQL Server provider options on a non-generic builder (runtime DI).
    /// </summary>
    public static void ConfigureFixturesSqlServer(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString)
    {
        optionsBuilder.UseSqlServer(connectionString, ConfigureSqlServer);
    }

    private static void ConfigureSqlServer(SqlServerDbContextOptionsBuilder sqlOptions)
    {
        sqlOptions.UseNetTopologySuite();
        sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");

        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    }
}
