using FluentAssertions;
using Sporeo.BuildingBlocks.Domain.Models;

namespace Sporeo.BuildingBlocks.Domain.Tests.Models;

public class EntityTests
{
    private sealed class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) : base(id) { }

        public TestEntity() { }
    }

    private sealed class OtherEntity : Entity<Guid>
    {
        public OtherEntity(Guid id) : base(id) { }
    }

    [Fact]
    public void Equals_WithSameIdAndType_ShouldBeTrue()
    {
        var id = Guid.NewGuid();
        var left = new TestEntity(id);
        var right = new TestEntity(id);

        left.Should().Be(right);
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentTypes_ShouldBeFalse()
    {
        var id = Guid.NewGuid();
        var left = new TestEntity(id);
        var right = new OtherEntity(id);

        left.Equals(right).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithTransientEntities_ShouldOnlyMatchSameReference()
    {
        var left = new TestEntity();
        var right = new TestEntity();

        left.Should().NotBe(right);
        (left == right).Should().BeFalse();
    }

    [Fact]
    public void Equals_TransientEntityWithItself_ShouldBeTrue()
    {
        var entity = new TestEntity();

        entity.Should().Be(entity);
    }

    [Fact]
    public void GetHashCode_ForTransientEntities_ShouldUseReferenceEquality()
    {
        var left = new TestEntity();
        var right = new TestEntity();

        left.GetHashCode().Should().NotBe(right.GetHashCode());
    }
}
