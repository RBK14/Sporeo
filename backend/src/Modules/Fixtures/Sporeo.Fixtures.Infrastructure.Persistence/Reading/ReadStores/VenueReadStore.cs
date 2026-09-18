using Dapper;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.Fixtures.Application.Venues.Data;
using Sporeo.Fixtures.Application.Venues.Queries.GetVenueDetails;
using Sporeo.Fixtures.Domain.Venues.ValueObjects;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Reading.ReadStores;

internal sealed class VenueReadStore(ISqlConnectionFactory sqlConnectionFactory) : IVenueReadStore
{
    public async Task<VenueDetailsReadModel?> GetVenueDetailsAsync(
        VenueId venueId,
        CancellationToken cancellationToken = default)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                v.Id,
                v.Name,
                v.Street,
                v.City,
                v.Country,
                CAST(v.Latitude AS float) AS Latitude,
                CAST(v.Longitude AS float) AS Longitude
            FROM venues v
            WHERE v.Id = @VenueId AND v.IsDeleted = 0
            """;

        return await connection.QueryFirstOrDefaultAsync<VenueDetailsReadModel>(
            new CommandDefinition(sql, new { VenueId = venueId }, cancellationToken: cancellationToken));
    }
}
