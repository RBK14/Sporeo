using Dapper;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.Fixtures.Application.Leagues.Abstractions.ReadStores;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;

namespace Sporeo.Fixtures.Infrastructure.Persistence.ReadStores;
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

    public async Task<IReadOnlyList<MonitoredLeagueForSyncReadModel>> GetMonitoredLeaguesForSyncAsync(CancellationToken cancellationToken = default)
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

        var result = await connection.QueryAsync<MonitoredLeagueForSyncReadModel>(
            new CommandDefinition(
                sql,
                cancellationToken: cancellationToken));

        return result.ToList();
    }

    public async Task<IReadOnlyList<LeagueWithCurrentSeasonReadModel>> GetMonitoredLeaguesWithCurrentSeasonAsync(CancellationToken cancellationToken = default)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
        SELECT 
            l.Id AS LeagueId,
            l.ExternalProviderName,
            l.ExternalProviderId,
            s.Name AS CurrentSeasonName
        FROM Leagues l
        INNER JOIN seasons s ON s.LeagueId = l.Id
          AND s.IsCurrent = 1
          AND s.IsDeleted = 0
        WHERE l.IsMonitored = 1
          AND l.IsDeleted = 0
          AND l.ExternalProviderId IS NOT NULL
        """;

        var result = await connection.QueryAsync<LeagueWithCurrentSeasonReadModel>(
            new CommandDefinition(
                sql,
                cancellationToken: cancellationToken));

        return result.ToList();
    }

    public async Task<LeagueExternalProviderDataReadModel?> GetLeagueExternalProviderDataAsync(LeagueId leagueId, CancellationToken cancellationToken = default)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
        SELECT 
            l.ExternalProviderName,
            l.ExternalProviderId
        FROM Leagues l
        WHERE l.Id = @LeagueId
          AND l.IsDeleted = 0
        """;

        return await connection.QueryFirstOrDefaultAsync<LeagueExternalProviderDataReadModel>(
            new CommandDefinition(sql, new { LeagueId = leagueId }, cancellationToken: cancellationToken));
    }
}
