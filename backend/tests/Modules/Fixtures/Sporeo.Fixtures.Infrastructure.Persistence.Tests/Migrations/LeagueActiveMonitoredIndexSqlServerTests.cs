using FluentAssertions;
using Microsoft.Data.SqlClient;
using Sporeo.Fixtures.Infrastructure.Persistence;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Tests;

[Collection(SqlServerCollection.Name)]
public sealed class LeagueActiveMonitoredIndexSqlServerTests
{
    private const string DatabaseName = "SporeoFixturesLeagueIndexTests";

    [Fact]
    public async Task Migration_ReplacesActiveMonitoredIndex_WithSportIdFilteredIndex()
    {
        await using var provider = await SqlServerTestDatabase.CreateInitializedProviderAsync(DatabaseName);
        var connectionString = SqlServerTestDatabase.CreateConnectionString(DatabaseName);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var newIndexCommand = connection.CreateCommand();
        newIndexCommand.CommandText = """
            SELECT i.name, COL_NAME(ic.object_id, ic.column_id) AS ColumnName
            FROM sys.indexes AS i
            INNER JOIN sys.tables AS t ON i.object_id = t.object_id
            INNER JOIN sys.index_columns AS ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
            WHERE t.name = N'leagues'
              AND i.name = N'IX_leagues_ActiveMonitored_SportId'
            """;

        await using var reader = await newIndexCommand.ExecuteReaderAsync();
        reader.Read().Should().BeTrue();
        reader.GetString(0).Should().Be("IX_leagues_ActiveMonitored_SportId");
        reader.GetString(1).Should().Be("SportId");
        reader.Read().Should().BeFalse();
        await reader.DisposeAsync();

        await using var oldIndexCommand = connection.CreateCommand();
        oldIndexCommand.CommandText = """
            SELECT i.name
            FROM sys.indexes AS i
            INNER JOIN sys.tables AS t ON i.object_id = t.object_id
            WHERE t.name = N'leagues' AND i.name = N'IX_Leagues_ActiveMonitored'
            """;

        var oldIndexName = await oldIndexCommand.ExecuteScalarAsync();
        oldIndexName.Should().BeNull();

        await using var fkIndexCommand = connection.CreateCommand();
        fkIndexCommand.CommandText = """
            SELECT i.name
            FROM sys.indexes AS i
            INNER JOIN sys.tables AS t ON i.object_id = t.object_id
            WHERE t.name = N'leagues' AND i.name = N'IX_leagues_SportId'
            """;

        var fkIndexName = (string?)await fkIndexCommand.ExecuteScalarAsync();
        fkIndexName.Should().Be("IX_leagues_SportId");
    }
}
