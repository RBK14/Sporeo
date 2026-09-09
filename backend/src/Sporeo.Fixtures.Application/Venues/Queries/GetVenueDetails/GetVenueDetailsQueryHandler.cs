using Dapper;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Domain.Common;

namespace Sporeo.Fixtures.Application.Venues.Queries.GetVenueDetails;

internal sealed class GetVenueDetailsQueryHandler(ISqlConnectionFactory sqlConnectionFactory) : IQueryHandler<GetVenueDetailsQuery, VenueDetailsResponse>
{
    public async Task<Result<VenueDetailsResponse>> Handle(GetVenueDetailsQuery request, CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                v.Id,
                v.Name,
                v.Street,
                v.City,
                v.Country,
                v.Latitude,
                v.Longitude
            FROM Venues v
            WHERE v.Id = @VenueId AND v.IsDeleted = 0
            """;

        var command = new CommandDefinition(
            sql,
            new { request.VenueId },
            cancellationToken: cancellationToken);

        var venue = await connection.QueryFirstOrDefaultAsync<VenueDetailsResponse>(command);

        if (venue == null)
            return Result.Failure<VenueDetailsResponse>(Errors.Venue.NotFound(request.VenueId));

        return Result.Success(venue);
    }
}
