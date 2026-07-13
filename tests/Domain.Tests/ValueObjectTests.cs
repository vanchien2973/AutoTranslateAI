using Domain.Common;
using FluentAssertions;
using Xunit;

namespace Domain.Tests;

public class ValueObjectTests
{
    private sealed class Money : ValueObject
    {
        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        public decimal Amount { get; }
        public string Currency { get; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    private sealed class Other : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return 1;
        }
    }

    [Fact]
    public void SameComponents_AreEqual()
    {
        var a = new Money(10, "USD");
        var b = new Money(10, "USD");

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void DifferentComponents_AreNotEqual()
    {
        (new Money(10, "USD") == new Money(10, "EUR")).Should().BeFalse();
        (new Money(10, "USD") != new Money(11, "USD")).Should().BeTrue();
    }

    [Fact]
    public void DifferentTypeOrNull_AreNotEqual()
    {
        new Money(10, "USD").Equals(new Other()).Should().BeFalse();
        new Money(10, "USD").Equals(null).Should().BeFalse();
    }
}
