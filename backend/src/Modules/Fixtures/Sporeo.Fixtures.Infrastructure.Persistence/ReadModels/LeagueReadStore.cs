using Dapper;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.Fixtures.Application.Leagues.Abstractions.ReadModels;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;

namespace Sporeo.Fixtures.Infrastructure.Persistence.ReadModels;
internal sealed class LeagueReadStore (ISqlConnectionFactory sqlConnectionFactory) : ILeagueReadStore
{
    private sealed record LeagueStatusDto(string ExternalProviderId, Guid Id, bool IsMonitored);

    public async Task<IReadOnlyDictionary<string, (LeagueId Id, bool IsMonitored)>> GetLeagueStatusesAsync(
        string providerName,
        IEnumerable<string> leagueProviderIds,
        CancellationToken cancellationToken = default)
    {
        var providerIds = leagueProviderIds.ToList();
        if (providerIds.Count == 0)
            return new Dictionary<string, (LeagueId, bool)>();

        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
            SELECT 
                l.ExternalProviderId,
                l.Id,
                l.IsMonitored
            FROM Leagues l
            WHERE ExternalProviderName = @ProviderName 
              AND ExternalProviderId IN @ProviderIds
              AND IsDeleted = 0
            """;

        var result = await connection.QueryAsync<LeagueStatusDto>(
            new CommandDefinition(
                sql,
                new { ProviderName = providerName, ProviderIds = providerIds},
                cancellationToken: cancellationToken));

        return result.ToDictionary(
            x => x.ExternalProviderId,
            x => (LeagueId.FromValue(x.Id), x.IsMonitored));

    }

    public async Task<IReadOnlyList<MonitoredLeagueForSyncDto>> GetMonitoredLeaguesForSyncAsync(CancellationToken cancellationToken = default)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
        SELECT 
            l.Id AS LeagueId,
            l.SportId,
            l.ExternalProviderName,
            l.ExternalProviderId
        FROM Leagues l
        WHERE IsMonitored = 1
          AND IsDeleted = 0
        """;

        var result = await connection.QueryAsync<MonitoredLeagueForSyncDto>(
            new CommandDefinition(
                sql,
                cancellationToken: cancellationToken));

        return result.ToList();
    }
}
