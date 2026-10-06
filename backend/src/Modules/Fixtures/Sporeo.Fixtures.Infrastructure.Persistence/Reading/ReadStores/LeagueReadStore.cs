using Dapper;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.Fixtures.Application.Leagues.Data;
using Sporeo.Fixtures.Application.Leagues.Queries.GetActiveLeagues;
using Sporeo.Fixtures.Application.Leagues.ReadModels;
using Sporeo.Fixtures.Domain.Leagues.ValueObjects;
using Sporeo.Fixtures.Domain.Seasons.ValueObjects;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Reading.ReadStores;

internal sealed class LeagueReadStore(ISqlConnectionFactory sqlConnectionFactory) : ILeagueReadStore
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

    public async Task<IReadOnlyList<ActiveLeagueReadModel>> GetActiveLeaguesAsync(
        SportId? sportId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        var whereClauses = new List<string>
        {
            "l.IsMonitored = 1",
            "l.IsDeleted = 0"
        };

        if (sportId is not null)
        {
            whereClauses.Add("l.SportId = @SportId");
            parameters.Add("SportId", sportId);
        }

        var whereSql = string.Join(" AND ", whereClauses);

        var sql = $"""
            SELECT
                l.Id,
                l.Name,
                l.Country,
                s.Id AS SportId,
                s.Name AS SportName,
                sea.Id AS CurrentSeasonId,
                sea.Name AS CurrentSeasonName
            FROM leagues l
            INNER JOIN sports s ON l.SportId = s.Id AND s.IsDeleted = 0
            LEFT JOIN seasons sea ON sea.LeagueId = l.Id AND sea.IsCurrent = 1 AND sea.IsDeleted = 0
            WHERE {whereSql}
            ORDER BY s.Name ASC, l.Name ASC
            """;

        var rows = await connection.QueryAsync<ActiveLeagueRow>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        return rows.Select(MapActiveLeague).ToList();
    }

    private sealed class ActiveLeagueRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
        public string? Country { get; init; }
        public Guid SportId { get; init; }
        public string SportName { get; init; } = null!;
        public Guid? CurrentSeasonId { get; init; }
        public string? CurrentSeasonName { get; init; }
    }

    private static ActiveLeagueReadModel MapActiveLeague(ActiveLeagueRow row) =>
        new(
            LeagueId.FromValue(row.Id),
            row.Name,
            row.Country,
            SportId.FromValue(row.SportId),
            row.SportName,
            row.CurrentSeasonId is null ? null : SeasonId.FromValue(row.CurrentSeasonId.Value),
            row.CurrentSeasonName);
}
