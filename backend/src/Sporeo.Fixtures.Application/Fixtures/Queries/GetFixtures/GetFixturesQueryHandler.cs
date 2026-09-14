using Dapper;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Application.Pagination;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Application.Fixtures.Queries.Common;

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

        var whereClauses = new List<string>
        {
            "f.IsDeleted = 0",
            "s.IsDeleted = 0"
        };

        FixtureFilterSql.Append(whereClauses, parameters, request.Filters);

        string whereSql = string.Join(" AND ", whereClauses);

        string sql = $"""
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
