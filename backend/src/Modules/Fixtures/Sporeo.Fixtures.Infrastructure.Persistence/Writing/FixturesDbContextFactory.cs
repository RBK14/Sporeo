using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Sporeo.BuildingBlocks.Infrastructure.Messaging.Outbox.Serialization;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Writing;

/// <summary>
/// Design-time factory for EF Core tools (<c>dotnet ef migrations</c>).
/// Prefer <c>ConnectionStrings__fixtures-db</c> when set; otherwise uses a local placeholder
/// that is never used for a live connection during <c>migrations add</c> / <c>script</c>.
/// </summary>
public sealed class FixturesDbContextFactory : IDesignTimeDbContextFactory<FixturesDbContext>
{
    private const string DesignTimeConnectionString =
        "Server=localhost;Database=fixtures-db;Trusted_Connection=True;TrustServerCertificate=True";

    /// <inheritdoc />
    public FixturesDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__fixtures-db")
            ?? DesignTimeConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<FixturesDbContext>();
        optionsBuilder.ConfigureFixturesSqlServer(connectionString);

        return new FixturesDbContext(
            optionsBuilder.Options,
            new DomainEventTypeRegistry([]));
    }
}
