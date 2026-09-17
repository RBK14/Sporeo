using Sporeo.Fixtures.Infrastructure.Persistence.Context;
using Dapper;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.Fixtures.Application.Fixtures.Abstractions.ReadModels;
using Sporeo.Fixtures.Application.Fixtures.Queries.Common;
using Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtureDetails;
using Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtures;
using Sporeo.Fixtures.Application.Fixtures.Queries.GetNearbyFixtures;
using Sporeo.Fixtures.Domain.Fixtures.Enums;

namespace Sporeo.Fixtures.Infrastructure.Persistence.ReadModels;

internal sealed class FixtureReadStore(ISqlConnectionFactory sqlConnectionFactory) : IFixtureReadStore
{
    public async Task<PagedResult<FixtureListItemResponse>> GetFixturesAsync(
        FixtureFilters filters,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("Offset", pagination.Offset);
        parameters.Add("PageSize", pagination.PageSize);

        var whereClauses = new List<string>
        {
            "f.IsDeleted = 0",
            "s.IsDeleted = 0"
        };

        FixtureFilterSql.Append(whereClauses, parameters, filters);
        var whereSql = string.Join(" AND ", whereClauses);

        var sql = $"""
            SELECT COUNT(*)
            FROM fixtures f
            INNER JOIN sports s ON f.SportId = s.Id
            LEFT JOIN leagues l ON f.LeagueId = l.Id AND l.IsDeleted = 0
            WHERE {whereSql};

            SELECT
                f.Id,
                f.Name,
                f.StartDate,
                s.Name AS SportName,
                l.Name AS LeagueName
            FROM fixtures f
            INNER JOIN sports s ON f.SportId = s.Id
            LEFT JOIN leagues l ON f.LeagueId = l.Id AND l.IsDeleted = 0
            WHERE {whereSql}
            ORDER BY f.StartDate ASC, f.Id ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        var totalCount = await multi.ReadFirstAsync<int>();
        var items = await multi.ReadAsync<FixtureListItemResponse>();
        return new PagedResult<FixtureListItemResponse>(items, totalCount, pagination);
    }

    public async Task<PagedResult<NearbyFixtureListItemResponse>> GetNearbyFixturesAsync(
        double latitude,
        double longitude,
        double radiusInMeters,
        FixtureFilters filters,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("Latitude", latitude);
        parameters.Add("Longitude", longitude);
        parameters.Add("RadiusInMeters", radiusInMeters);
        parameters.Add("Offset", pagination.Offset);
        parameters.Add("PageSize", pagination.PageSize);

        var whereClauses = new List<string>
        {
            "f.IsDeleted = 0",
            "s.IsDeleted = 0",
            "v.IsDeleted = 0",
            "v.Location.STDistance(@Origin) <= @RadiusInMeters"
        };

        FixtureFilterSql.Append(whereClauses, parameters, filters);
        var whereSql = string.Join(" AND ", whereClauses);

        var sql = $"""
            DECLARE @Origin geography = geography::Point(@Latitude, @Longitude, 4326);

            SELECT COUNT(*)
            FROM fixtures f
            INNER JOIN venues v ON f.VenueId = v.Id
            INNER JOIN sports s ON f.SportId = s.Id
            LEFT JOIN leagues l ON f.LeagueId = l.Id AND l.IsDeleted = 0
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
            LEFT JOIN leagues l ON f.LeagueId = l.Id AND l.IsDeleted = 0
            WHERE {whereSql}
            ORDER BY v.Location.STDistance(@Origin) ASC, f.StartDate ASC, f.Id ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        var totalCount = await multi.ReadFirstAsync<int>();
        var items = await multi.ReadAsync<NearbyFixtureListItemResponse>();
        return new PagedResult<NearbyFixtureListItemResponse>(items, totalCount, pagination);
    }

    public async Task<FixtureDetailsResponse?> GetFixtureDetailsAsync(
        Guid fixtureId,
        CancellationToken cancellationToken = default)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                f.Id,
                f.Name,
                f.StartDate,
                f.Status,
                v.Id AS VenueId,
                v.Name AS VenueName,
                v.Street AS VenueStreet,
                v.City AS VenueCity,
                v.Country AS VenueCountry,
                spo.Id AS SportId,
                spo.Name AS SportName,
                l.Id AS LeagueId,
                l.Name AS LeagueName,
                sea.Id AS SeasonId,
                sea.Name AS SeasonName
            FROM fixtures f
            LEFT JOIN venues v ON f.VenueId = v.Id AND v.IsDeleted = 0
            INNER JOIN sports spo ON f.SportId = spo.Id AND spo.IsDeleted = 0
            LEFT JOIN leagues l ON f.LeagueId = l.Id AND l.IsDeleted = 0
            LEFT JOIN seasons sea ON f.SeasonId = sea.Id AND sea.IsDeleted = 0
            WHERE f.Id = @FixtureId AND f.IsDeleted = 0
            """;

        var row = await connection.QuerySingleOrDefaultAsync<FixtureDetailsRow>(
            new CommandDefinition(sql, new { FixtureId = fixtureId }, cancellationToken: cancellationToken));

        return row is null ? null : MapFixtureDetails(row);
    }

    private sealed class FixtureDetailsRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
        public DateTimeOffset StartDate { get; init; }
        public FixtureStatus Status { get; init; }
        public Guid? VenueId { get; init; }
        public string? VenueName { get; init; }
        public string? VenueStreet { get; init; }
        public string? VenueCity { get; init; }
        public string? VenueCountry { get; init; }
        public Guid SportId { get; init; }
        public string SportName { get; init; } = null!;
        public Guid? LeagueId { get; init; }
        public string? LeagueName { get; init; }
        public Guid? SeasonId { get; init; }
        public string? SeasonName { get; init; }
    }

    private static FixtureDetailsResponse MapFixtureDetails(FixtureDetailsRow row)
    {
        FixtureVenueDto? venue = row.VenueId is null
            ? null
            : new FixtureVenueDto(row.VenueId.Value, row.VenueName!, row.VenueStreet, row.VenueCity, row.VenueCountry);

        FixtureLeagueDto? league = row.LeagueId is null
            ? null
            : new FixtureLeagueDto(row.LeagueId.Value, row.LeagueName!);

        FixtureSeasonDto? season = row.SeasonId is null
            ? null
            : new FixtureSeasonDto(row.SeasonId.Value, row.SeasonName!);

        return new FixtureDetailsResponse(
            row.Id,
            row.Name,
            row.StartDate,
            row.Status,
            venue,
            new FixtureSportDto(row.SportId, row.SportName),
            league,
            season);
    }
}
