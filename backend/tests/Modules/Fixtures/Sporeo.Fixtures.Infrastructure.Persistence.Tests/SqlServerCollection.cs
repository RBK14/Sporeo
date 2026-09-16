using Xunit;

namespace Sporeo.Fixtures.Infrastructure.Persistence.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SqlServerCollection : ICollectionFixture<object>
{
    public const string Name = "SqlServerLocalDb";
}
