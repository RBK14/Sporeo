using System.Data;

namespace Sporeo.BuildingBlocks.Application.Abstractions.Data;

/// <summary>
/// Creates ADO.NET database connections for read-side query handlers.
/// </summary>
public interface ISqlConnectionFactory
{
    /// <summary>
    /// Creates a new, unopened database connection.
    /// </summary>
    /// <returns>A new <see cref="IDbConnection"/> instance that the caller must dispose.</returns>
    IDbConnection CreateConnection();
}
