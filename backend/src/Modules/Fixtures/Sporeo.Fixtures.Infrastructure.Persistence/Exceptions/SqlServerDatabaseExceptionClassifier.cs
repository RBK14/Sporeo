using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Sporeo.Fixtures.Application.Abstractions.Persistence;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Exceptions;

/// <summary>
/// Classifies SQL Server exceptions by error number for unique constraint handling.
/// </summary>
internal sealed class SqlServerDatabaseExceptionClassifier : IDatabaseExceptionClassifier
{
    private static readonly HashSet<int> UniqueViolationErrorNumbers = [2601, 2627];

    /// <inheritdoc />
    public bool IsUniqueConstraintViolation(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sqlException &&
                UniqueViolationErrorNumbers.Contains(sqlException.Number))
            {
                return true;
            }

            if (current is DbUpdateException &&
                current.InnerException is SqlException innerSql &&
                UniqueViolationErrorNumbers.Contains(innerSql.Number))
            {
                return true;
            }
        }

        return false;
    }
}
