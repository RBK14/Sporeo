using Microsoft.Data.SqlClient;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using System.Data;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Connections;

internal sealed class SqlConnectionFactory(string connectionString) : ISqlConnectionFactory
{
    public IDbConnection CreateConnection()
    {
        var connection = new SqlConnection(connectionString);

        return connection;
    }
}
