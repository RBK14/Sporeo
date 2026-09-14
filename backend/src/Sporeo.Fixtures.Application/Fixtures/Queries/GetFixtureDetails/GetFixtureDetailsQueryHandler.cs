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

        var command = new CommandDefinition(
            sql,
            new { request.FixtureId },
            cancellationToken: cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<FixtureDetailsRow>(command);

        if (row is null)
            return Result.Failure<FixtureDetailsResponse>(Errors.Fixture.NotFound(request.FixtureId));

        return Result.Success(MapFixtureDetails(row));
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
            : new FixtureVenueDto(
                row.VenueId.Value,
                row.VenueName!,
                row.VenueStreet,
                row.VenueCity,
                row.VenueCountry);

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
