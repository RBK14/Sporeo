using Microsoft.EntityFrameworkCore;

namespace Sporeo.Fixtures.Infrastructure.Persistence;

public class FixturesDbContext(DbContextOptions<FixturesDbContext> options) : DbContext(options), IUnit
{

}
