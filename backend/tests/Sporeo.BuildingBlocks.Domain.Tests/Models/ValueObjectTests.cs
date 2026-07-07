using FluentAssertions;
using Sporeo.BuildingBlocks.Domain.Models;

namespace Sporeo.BuildingBlocks.Domain.Tests.Models;

public class ValueObjectTests
{
    private sealed class Money : ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    [Fact]
    public void Equals_WithSameComponents_ShouldBeTrue()
    {
        var left = new Money(10m, "PLN");
        var right = new Money(10m, "PLN");

        left.Should().Be(right);
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentComponents_ShouldBeFalse()
    {
        var left = new Money(10m, "PLN");
        var right = new Money(20m, "PLN");

        left.Should().NotBe(right);
    }
}
