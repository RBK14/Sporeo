namespace Sporeo.Fixtures.Application.Abstractions.Persistence;

/// <summary>
/// Classifies database exceptions for application-level retry and skip decisions.
/// </summary>
public interface IDatabaseExceptionClassifier
{
    /// <summary>
    /// Determines whether the exception represents a unique constraint or unique index violation
    /// that may be treated as a retriable race condition.
    /// </summary>
    /// <param name="exception">The exception to inspect, including nested inner exceptions.</param>
    /// <returns><see langword="true"/> when the exception is a unique key/index violation.</returns>
    bool IsUniqueConstraintViolation(Exception exception);
}
