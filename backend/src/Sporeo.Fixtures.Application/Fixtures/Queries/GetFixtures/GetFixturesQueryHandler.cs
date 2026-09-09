using Dapper;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtures;

internal sealed class GetFixturesQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory) : IQueryHandler<GetFixturesQuery, PagedResult<FixtureListItemResponse>>
{
    public async Task<Result<PagedResult<FixtureListItemResponse>>> Handle(GetFixturesQuery request, CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("Offset", request.Pagination.Offset);
        parameters.Add("PageSize", request.Pagination.PageSize);

        var whereClauses = new List<string> { "f.IsDeleted = 0" };

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

        string sql = $"""
            SELECT COUNT (*)
            FROM Fixtures f
            WHERE {whereSql};

            SELECT
                f.Id,
                f.Name,
                f.StartDate,
                s.Name AS SportName,
                l.Name AS LeagueName
            FROM Fixtures f
            INNER JOIN Sports s ON f.SportId = s.Id
            LEFT JOIN Leagues l ON f.LeagueId = l.Id
            WHERE {whereSql}
            ORDER BY f.StartDate ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var command = new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken);

        using var multi = await connection.QueryMultipleAsync(command);

        var totalCount = await multi.ReadFirstAsync<int>();
        var items = await multi.ReadAsync<FixtureListItemResponse>();

        var pagedResult = new PagedResult<FixtureListItemResponse>(
            items,
            totalCount,
            request.Pagination);

        return Result.Success(pagedResult);
    }
}
