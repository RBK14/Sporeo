using Dapper;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetNearbyFixtures;

internal sealed class GetNearbyFixturesQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory) : IQueryHandler<GetNearbyFixturesQuery, PagedResult<NearbyFixtureListItemResponse>>
{
    public async Task<Result<PagedResult<NearbyFixtureListItemResponse>>> Handle(GetNearbyFixturesQuery request, CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("Latitude", request.Latitude);
        parameters.Add("Longitude", request.Longitude);
        parameters.Add("RadiusInMeters", request.RadiusInMeters);
        parameters.Add("Offset", request.Pagination.Offset);
        parameters.Add("PageSize", request.Pagination.PageSize);

        var whereClauses = new List<string>
        {
            "f.IsDeleted = 0",
            "v.IsDeleted = 0",
            "v.Location.STDistance(@Origin) <= @RadiusInMeters"
        };

        if (request.Filters.SportId.HasValue)
        {
            whereClauses.Add("f.SportId = @SportId");
            parameters.Add("SportId", request.Filters.SportId.Value);
        }

        if (request.Filters.LeagueId.HasValue)
        {
            whereClauses.Add("f.LeagueId = @LeagueId");
            parameters.Add("LeagueId", request.Filters.LeagueId.Value);
        }

        if (request.Filters.DateFrom.HasValue)
        {
            whereClauses.Add("f.StartDate >= @DateFrom");
            parameters.Add("DateFrom", request.Filters.DateFrom.Value);
        }

        if (request.Filters.DateTo.HasValue)
        {
            whereClauses.Add("f.StartDate <= @DateTo");
            parameters.Add("DateTo", request.Filters.DateTo.Value);
        }

        string whereSql = string.Join(" AND ", whereClauses);

        // Declare the @Origin variable in the SQL query to avoid SQL injection and ensure proper parameterization
        // Create a geography point from the latitude and longitude geography::Point(Latitude, Longitude, SRID)
        string sql = $"""
            DECLARE @Origin geography = geography::Point(@Latitude, @Longitude, 4326);

            SELECT COUNT(*) 
            FROM fixtures f
            INNER JOIN venues v ON f.VenueId = v.Id
            WHERE {whereSql};

            SELECT 
                f.Id, 
                f.Name, 
                f.StartDate, 
                s.Name AS SportName,
                l.Name AS LeagueName,
                v.Location.STDistance(@Origin) AS DistanceInMeters
            FROM fixtures f
            INNER JOIN venues v ON f.VenueId = v.Id
            INNER JOIN sports s ON f.SportId = s.Id
            LEFT JOIN leagues l ON f.LeagueId = l.Id
            WHERE {whereSql}
            ORDER BY v.Location.STDistance(@Origin) ASC, f.StartDate ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var command = new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken);

        using var multi = await connection.QueryMultipleAsync(command);

        var totalCount = await multi.ReadFirstAsync<int>();
        var items = await multi.ReadAsync<NearbyFixtureListItemResponse>();

        var pagedResult = new PagedResult<NearbyFixtureListItemResponse>(
            items,
            totalCount,
            request.Pagination);

        return Result.Success(pagedResult);
    }
}
