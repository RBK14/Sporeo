using Microsoft.Extensions.Logging;

namespace Sporeo.BuildingBlocks.Application.Extensions;

/// <summary>
/// Provides high-performance logging extension methods used by MediatR pipeline behaviors.
/// </summary>
public static partial class BehaviorsLoggerExtensions
{
    /// <summary>
    /// Logs that request processing has started.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="requestName">The name of the request being processed.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Processing request: {RequestName}.")]
    public static partial void LogProcessingRequest(this ILogger logger, string requestName);

    /// <summary>
    /// Logs that request processing has completed successfully.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="requestName">The name of the request that was processed.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Finished processing request: {RequestName}.")]
    public static partial void LogProcessedRequest(this ILogger logger, string requestName);

    /// <summary>
    /// Logs that request processing failed due to an unexpected exception.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="ex">The exception that caused the failure.</param>
    /// <param name="requestName">The name of the request that failed.</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "System error occurred while processing request: {RequestName}.")]
    public static partial void LogProcessingRequestFailed(this ILogger logger, Exception ex, string requestName);

    /// <summary>
    /// Logs that a request exceeded the configured performance threshold.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="requestName">The name of the long-running request.</param>
    /// <param name="elapsedMilliseconds">The elapsed time in milliseconds.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Long running request: {RequestName} ({ElapsedMilliseconds} ms).")]
    public static partial void LogLongRunningRequest(this ILogger logger, string requestName, long elapsedMilliseconds);

    /// <summary>
    /// Logs that a unit-of-work commit is about to be attempted for a command.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="commandName">The name of the command being executed.</param>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Beginning unit-of-work commit for: {CommandName}.")]
    public static partial void LogBeginUnitOfWork(this ILogger logger, string commandName);

    /// <summary>
    /// Logs that pending unit-of-work changes were committed for a command.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="commandName">The name of the command that was committed.</param>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Committed unit-of-work changes for: {CommandName}.")]
    public static partial void LogCommitUnitOfWork(this ILogger logger, string commandName);

    /// <summary>
    /// Logs that unit-of-work changes were not committed because the command returned a domain failure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="commandName">The name of the command that failed.</param>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipped unit-of-work commit for: {CommandName} due to domain error.")]
    public static partial void LogSkippedUnitOfWorkCommit(this ILogger logger, string commandName);

    /// <summary>
    /// Logs that a cached query response was served from the cache.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="cacheKeyHash">A masked or hashed representation of the cache key.</param>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache hit for key hash: {CacheKeyHash}.")]
    public static partial void LogCacheHit(this ILogger logger, string cacheKeyHash);

    /// <summary>
    /// Logs that a cached query response was not found and will be executed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="cacheKeyHash">A masked or hashed representation of the cache key.</param>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache miss for key hash: {CacheKeyHash}. Processing query.")]
    public static partial void LogCacheMiss(this ILogger logger, string cacheKeyHash);

    /// <summary>
    /// Returns a non-reversible hash suitable for logging cache keys without exposing sensitive values.
    /// </summary>
    /// <param name="cacheKey">The full cache key.</param>
    /// <returns>A short hexadecimal hash of the cache key.</returns>
    public static string ToLogSafeCacheKeyHash(string cacheKey)
    {
        var hash = cacheKey.GetHashCode(StringComparison.Ordinal);
        return hash.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
    }
}
