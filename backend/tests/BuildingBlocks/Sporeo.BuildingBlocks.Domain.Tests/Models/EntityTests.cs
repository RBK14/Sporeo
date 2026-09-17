using FluentAssertions;
using Sporeo.BuildingBlocks.Domain.Models;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.BuildingBlocks.Domain.Rules;

namespace Sporeo.BuildingBlocks.Domain.Tests.Models;

public class EntityTests
{
    private sealed class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) : base(id) { }

        public TestEntity() { }

        public static Result Validate(params IBusinessRule[] rules) => CheckRules(rules);
    }

    private sealed class BrokenRule : IBusinessRule
    {
        public bool IsBroken() => true;

        public Error Error { get; } = new("Test.Broken", "Rule is broken.");
    }

    private sealed class SatisfiedRule : IBusinessRule
    {
        public bool IsBroken() => false;

        public Error Error { get; } = new("Test.Satisfied", "Rule is satisfied.");
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

    [Fact]
    public void CheckRules_WithBrokenRule_ShouldReturnFailure()
    {
        var result = TestEntity.Validate(new BrokenRule());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Test.Broken");
    }

    [Fact]
    public void CheckRules_WithSatisfiedRules_ShouldReturnSuccess()
    {
        var result = TestEntity.Validate(new SatisfiedRule(), new SatisfiedRule());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CheckRules_ShouldReturnFirstFailure()
    {
        var result = TestEntity.Validate(new SatisfiedRule(), new BrokenRule(), new BrokenRule());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Test.Broken");
    }
}
