using Dapper;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.Fixtures.Application.Sports.Data;
using Sporeo.Fixtures.Application.Sports.Queries.GetActiveSports;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Reading.ReadStores;

internal sealed class SportReadStore(ISqlConnectionFactory sqlConnectionFactory) : ISportReadStore
{
    private sealed record SportStatusDto(string ExternalProviderId, Guid Id);

    public async Task<IReadOnlyDictionary<string, SportId>> GetSportStatusesAsync(
        string providerName,
        IEnumerable<string> sportProviderIds,
        CancellationToken cancellationToken = default)
    {
        var providerIds = sportProviderIds.ToList();
        if (providerIds.Count == 0)
            return new Dictionary<string, SportId>();

        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                s.ExternalProviderId,
                s.Id
            FROM Sports s
            WHERE ExternalProviderName = @ProviderName
              AND ExternalProviderId IN @ProviderIds
              AND IsDeleted = 0
            """;

        var result = await connection.QueryAsync<SportStatusDto>(
            new CommandDefinition(
                sql,
                new { ProviderName = providerName, ProviderIds = providerIds },
                cancellationToken: cancellationToken));

        return result.ToDictionary(
            x => x.ExternalProviderId,
            x => SportId.FromValue(x.Id));
    }

    public async Task<IReadOnlyList<ActiveSportReadModel>> GetActiveSportsAsync(
        CancellationToken cancellationToken = default)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                s.Id,
                s.Name
            FROM sports s
            WHERE s.IsDeleted = 0
              AND EXISTS (
                  SELECT 1
                  FROM leagues l
                  WHERE l.SportId = s.Id
                    AND l.IsMonitored = 1
                    AND l.IsDeleted = 0)
            ORDER BY s.Name ASC
            """;

        var rows = await connection.QueryAsync<ActiveSportRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows
            .Select(row => new ActiveSportReadModel(SportId.FromValue(row.Id), row.Name))
            .ToList();
    }

    private sealed record ActiveSportRow(Guid Id, string Name);
}
