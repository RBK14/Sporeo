using Microsoft.Extensions.Logging;

namespace Sporeo.Fixtures.Infrastructure.Persistence;

/// <summary>
/// Provides high-performance logging extension methods used by Fixtures persistence.
/// </summary>
public static partial class FixturesPersistenceLoggerExtensions
{
    /// <summary>
    /// Logs that an error occurred while applying database migrations.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="ex">The exception thrown during migration.</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred during database migration.")]
    public static partial void LogDatabaseMigrationError(this ILogger logger, Exception ex);
}
