using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sporeo.Fixtures.Application;
using Sporeo.Fixtures.Infrastructure.Persistence;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Tests;

internal static class SqlServerTestDatabase
{
    private const string MasterConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

    public static string CreateConnectionString(string databaseName) =>
        $"Server=(localdb)\\mssqllocaldb;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

    public static async Task EnsureLocalDbAvailableAsync()
    {
        try
        {
            await using var connection = new SqlConnection(MasterConnectionString);
            await connection.OpenAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "SQL Server LocalDB is required for fixtures query integration tests.", ex);
        }
    }

    public static async Task RecreateDatabaseAsync(string databaseName)
    {
        await using var connection = new SqlConnection(MasterConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID(N'{databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{databaseName}];
            END
            """;
        await command.ExecuteNonQueryAsync();
    }

    public static async Task<ServiceProvider> CreateInitializedProviderAsync(string databaseName)
    {
        await EnsureLocalDbAvailableAsync();
        await RecreateDatabaseAsync(databaseName);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:fixtures-db"] = CreateConnectionString(databaseName)
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddPersistence(configuration);

        var provider = services.BuildServiceProvider();
        await provider.InitializeFixturesDatabaseAsync();
        return provider;
    }
}
