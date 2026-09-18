using Dapper;
using Sporeo.Fixtures.Application.Fixtures.Queries.Common;

namespace Sporeo.Fixtures.Infrastructure.Persistence.ReadStores;

internal static class FixtureFilterSql
{
    public static void Append(
        List<string> whereClauses,
        DynamicParameters parameters,
        FixtureFilters filters)
    {
        if (filters.SportId is not null)
        {
            whereClauses.Add("f.SportId = @SportId");
            parameters.Add("SportId", filters.SportId);
        }

        if (filters.LeagueId is not null)
        {
            whereClauses.Add("f.LeagueId = @LeagueId");
            parameters.Add("LeagueId", filters.LeagueId);
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
