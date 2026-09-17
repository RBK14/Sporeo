using FluentAssertions;
using Sporeo.Fixtures.Domain.Common;
using SportAggregate = Sporeo.Fixtures.Domain.Sports.Sport;

namespace Sporeo.Fixtures.Domain.Tests.Sport;

public class SportTests
{
    [Fact]
    public void Create_WithOnlyName_ShouldNotRequireProviderMetadata()
    {
        var result = SportAggregate.Create("Football");

        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalProviderName.Should().BeNull();
        result.Value.ExternalProviderId.Should().BeNull();
    }

    [Fact]
    public void Create_WithPartialProviderMetadata_ShouldSucceed()
    {
        var result = SportAggregate.Create("Football", "ProviderA", null);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalProviderName.Should().Be("ProviderA");
        result.Value.ExternalProviderId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldCreateSport()
    {
        var result = SportAggregate.Create("Football");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Football");
        result.Value.ExternalProviderName.Should().BeNull();
        result.Value.ExternalProviderId.Should().BeNull();
        result.Value.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_WithOptionalProviderMetadata_ShouldPersistProviderFields()
    {
        var result = SportAggregate.Create("Football", "ProviderA", "external-123");

        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalProviderName.Should().Be("ProviderA");
        result.Value.ExternalProviderId.Should().Be("external-123");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldFail()
    {
        var result = SportAggregate.Create("  ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Sport.EmptyName);
    }

    [Fact]
    public void Update_ShouldChangeName()
    {
        var sport = SportAggregate.Create("Football").Value;

        var result = sport.Update("Soccer");

        result.IsSuccess.Should().BeTrue();
        sport.Name.Should().Be("Soccer");
    }

    [Fact]
    public void Update_WithEmptyName_ShouldFail()
    {
        var sport = SportAggregate.Create("Football").Value;

        var result = sport.Update("  ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Sport.EmptyName);
        sport.Name.Should().Be("Football");
    }

    [Fact]
    public void Delete_ShouldBeIdempotent()
    {
        var sport = SportAggregate.Create("Football").Value;

        sport.Delete().IsSuccess.Should().BeTrue();
        var secondDelete = sport.Delete();

        secondDelete.IsSuccess.Should().BeTrue();
        sport.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void DeletedSport_ShouldBlockUpdate()
    {
        var sport = SportAggregate.Create("Football").Value;
        sport.Delete().IsSuccess.Should().BeTrue();

        var result = sport.Update("Soccer");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Sport.Deleted);
        sport.Name.Should().Be("Football");
    }
}
