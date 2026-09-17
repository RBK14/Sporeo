using FluentAssertions;
using Sporeo.BuildingBlocks.Domain.Models;

namespace Sporeo.BuildingBlocks.Domain.Tests.Models;

public class TypedIdTests
{
    private sealed record TestId(Guid Value) : TypedIdBase(Value);

    [Fact]
    public void Constructor_WithEmptyGuid_ShouldThrow()
    {
        var act = () => new TestId(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Value_ShouldExposeGuid()
    {
        var value = Guid.NewGuid();
        var id = new TestId(value);

        id.Value.Should().Be(value);
    }

    [Fact]
    public void Equality_WithSameValue_ShouldBeTrue()
    {
        var value = Guid.NewGuid();
        var left = new TestId(value);
        var right = new TestId(value);

        left.Should().Be(right);
    }
}
