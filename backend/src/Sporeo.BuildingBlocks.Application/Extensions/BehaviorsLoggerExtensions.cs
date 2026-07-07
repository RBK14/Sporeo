using Microsoft.Extensions.Logging;

namespace Sporeo.BuildingBlocks.Application.Extensions;

public static partial class BehaviorsLoggerExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Processing request: {RequestName}.")]
    public static partial void LogProcessingRequest(this ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Finished processing request: {RequestName}.")]
    public static partial void LogProcessedRequest(this ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Error, Message = "System error occurred while processing request: {RequestName}.")]
    public static partial void LogProcessingRequestFailed(this ILogger logger, Exception ex, string requestName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Long running request: {RequestName} ({ElapsedMilliseconds} ms).")]
    public static partial void LogLongRunningRequest(this ILogger logger, string requestName, long elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Beginning transaction for: {CommandName}.")]
    public static partial void LogBeginTransaction(this ILogger logger, string commandName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Committed transaction for: {CommandName}.")]
    public static partial void LogCommitTransaction(this ILogger logger, string commandName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rolled back transaction for: {CommandName} due to domain error.")]
    public static partial void LogRollbackTransaction(this ILogger logger, string commandName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cache hit for key: {CacheKey}.")]
    public static partial void LogCacheHit(this ILogger logger, string cacheKey);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cache miss for key: {CacheKey}. Processing query.")]
    public static partial void LogCacheMiss(this ILogger logger, string cacheKey);
}
