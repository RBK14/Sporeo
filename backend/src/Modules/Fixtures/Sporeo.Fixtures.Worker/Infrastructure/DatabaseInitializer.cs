using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Sporeo.Fixtures.Infrastructure.Persistence;

namespace Sporeo.Fixtures.Worker.Infrastructure;

/// <summary>
/// Initializes Fixtures and Quartz databases for the Worker process.
/// </summary>
public static partial class DatabaseInitializer
{
    private const string QuartzSchemaScriptResourceName = "Sporeo.Fixtures.Worker.Scripts.quartz_schema.sql";

    /// <summary>
    /// Applies Fixtures EF Core migrations and ensures the Quartz schema exists.
    /// </summary>
    /// <param name="services">The root service provider.</param>
    /// <param name="logger">The logger used for initialization diagnostics.</param>
    /// <returns>A task that completes when both databases are initialized.</returns>
    public static async Task InitializeAsync(IServiceProvider services, ILogger logger)
    {
        try
        {
            logger.LogInformation("Starting Fixtures database migrations.");
            await services.InitializeFixturesDatabaseAsync();
            logger.LogInformation("Fixtures database migrations completed.");

            await InitializeQuartzDatabaseAsync(services, logger);
            logger.LogInformation("Database initialization completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the databases.");
            throw;
        }
    }

    private static async Task InitializeQuartzDatabaseAsync(IServiceProvider services, ILogger logger)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("quartz-db")
            ?? throw new InvalidOperationException("Connection string 'quartz-db' was not found.");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        if (await QuartzSchemaExistsAsync(connection))
        {
            logger.LogInformation("Quartz schema already exists; skipping schema script.");
            return;
        }

        logger.LogInformation("Quartz schema not found; applying schema script.");
        var script = await ReadEmbeddedQuartzSchemaScriptAsync();
        await ExecuteSqlBatchesAsync(connection, script);
        logger.LogInformation("Quartz schema script applied successfully.");
    }

    private static async Task<bool> QuartzSchemaExistsAsync(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CASE
                WHEN OBJECT_ID(N'[dbo].[QRTZ_JOB_DETAILS]', N'U') IS NULL THEN 0
                ELSE 1
            END
            """;

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) == 1;
    }

    private static async Task<string> ReadEmbeddedQuartzSchemaScriptAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream(QuartzSchemaScriptResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{QuartzSchemaScriptResourceName}' was not found.");

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private static async Task ExecuteSqlBatchesAsync(SqlConnection connection, string script)
    {
        foreach (var batch in SplitSqlBatches(script))
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            // Skip USE statements — the connection already targets quartz-db.
            if (UseStatementRegex().IsMatch(batch.Trim()))
            {
                continue;
            }

            await using var command = connection.CreateCommand();
            command.CommandText = batch;
            await command.ExecuteNonQueryAsync();
        }
    }

    private static IEnumerable<string> SplitSqlBatches(string script)
    {
        return GoBatchRegex().Split(script)
            .Select(batch => batch.Trim())
            .Where(batch => batch.Length > 0);
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex GoBatchRegex();

    [GeneratedRegex(@"^USE\s+\[.+\]\s*;?\s*$", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex UseStatementRegex();
}
