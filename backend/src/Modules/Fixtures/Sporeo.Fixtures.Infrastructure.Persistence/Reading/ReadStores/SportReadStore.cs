using Dapper;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.Fixtures.Application.Sports.Data;
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
}
