using Dapper;
using Sporeo.Fixtures.Application.Fixtures.Queries.Common;

namespace Sporeo.Fixtures.Infrastructure.Persistence.ReadModels;

internal static class FixtureFilterSql
{
    public static void Append(
        List<string> whereClauses,
        DynamicParameters parameters,
        FixtureFilters filters)
    {
        if (filters.SportId.HasValue)
        {
            whereClauses.Add("f.SportId = @SportId");
            parameters.Add("SportId", filters.SportId.Value);
        }

        if (filters.LeagueId.HasValue)
        {
            whereClauses.Add("f.LeagueId = @LeagueId");
            parameters.Add("LeagueId", filters.LeagueId.Value);
        }

        if (filters.DateFrom.HasValue)
        {
            whereClauses.Add("f.StartDate >= @DateFrom");
            parameters.Add("DateFrom", filters.DateFrom.Value);
        }

        if (filters.DateTo.HasValue)
        {
            whereClauses.Add("f.StartDate <= @DateTo");
            parameters.Add("DateTo", filters.DateTo.Value);
        }
    }
}
