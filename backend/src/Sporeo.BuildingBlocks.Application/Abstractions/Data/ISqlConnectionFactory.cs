using System.Data;

namespace Sporeo.BuildingBlocks.Application.Abstractions.Data;

public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
}
