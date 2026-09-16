using Sporeo.Fixtures.Infrastructure.Persistence.Data;
using Dapper;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.Fixtures.Application.Venues.Abstractions.ReadModels;
using Sporeo.Fixtures.Application.Venues.Queries.GetVenueDetails;

namespace Sporeo.Fixtures.Infrastructure.Persistence.ReadModels;

internal sealed class VenueReadStore(ISqlConnectionFactory sqlConnectionFactory) : IVenueReadStore
{
    public async Task<VenueDetailsResponse?> GetVenueDetailsAsync(
        Guid venueId,
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

        return await connection.QueryFirstOrDefaultAsync<VenueDetailsResponse>(
            new CommandDefinition(sql, new { VenueId = venueId }, cancellationToken: cancellationToken));
    }
}
