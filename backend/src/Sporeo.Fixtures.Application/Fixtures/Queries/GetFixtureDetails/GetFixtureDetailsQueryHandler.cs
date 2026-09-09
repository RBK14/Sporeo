using Dapper;
using Sporeo.BuildingBlocks.Application.Abstractions.Data;
using Sporeo.BuildingBlocks.Application.Abstractions.Execution;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Fixtures.Enums;

namespace Sporeo.Fixtures.Application.Fixtures.Queries.GetFixtureDetails;

internal sealed class GetFixtureDetailsQueryHandler(ISqlConnectionFactory sqlConnectionFactory) : IQueryHandler<GetFixtureDetailsQuery, FixtureDetailsResponse>
{
    public async Task<Result<FixtureDetailsResponse>> Handle(GetFixtureDetailsQuery request, CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                f.Id, f.Name, f.StartDate, f.Status,
                v.Id, v.Name, v.Street, v.City, v.Country,
                spo.Id, spo.Name,
                l.Id, l.Name,
                sea.Id, sea.Name

            FROM Fixtures f
            LEFT JOIN venues v ON f.VenueId = v.Id
            INNER JOIN sports spo ON f.SportId = spo.Id
            LEFT JOIN leagues l ON f.LeagueId = l.Id
            LEFT JOIN seasons sea ON f.SeasonId = sea.Id
            WHERE f.Id = @FixtureId AND f.IsDeleted = 0
            """;

        var command = new CommandDefinition(
            sql,
            new { request.FixtureId },
            cancellationToken: cancellationToken);

        var result = await connection.QueryAsync<
            FixtureRow,
            FixtureVenueDto,
            FixtureSportDto,
            FixtureLeagueDto,
            FixtureSeasonDto,
            FixtureDetailsResponse>(
            command,
            MapFixtureDetails,
            splitOn: "Id,Id,Id,Id");

        var fixtureResponse = result.FirstOrDefault();

        if (fixtureResponse is null)
            return Result.Failure<FixtureDetailsResponse>(Errors.Fixture.NotFound(request.FixtureId));

        return Result.Success(fixtureResponse);
    }

    private sealed record FixtureRow(
        Guid Id,
        string Name,
        DateTimeOffset StartDate,
        FixtureStatus Status);

    private static FixtureDetailsResponse MapFixtureDetails(
        FixtureRow fixture,
        FixtureVenueDto venue,
        FixtureSportDto sport,
        FixtureLeagueDto league,
        FixtureSeasonDto season)
    {
        return new FixtureDetailsResponse(
            fixture.Id,
            fixture.Name,
            fixture.StartDate,
            fixture.Status,
            venue,
            sport,
            league,
            season);
    }
}
