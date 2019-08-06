using FluentAssertions;
using Seedwork.CQRS.Bus.Core;
using Xunit;

namespace Seedwork.CQRS.Bus.UnitTests
{
    public class DurabilityTests
    {
        [Fact]
        public void GivenDurabilityWhenDurableShouldIsDurableBeFalse()
        {
            var durability = Durability.Transient;

            durability.Should().NotBeNull();
            durability.IsDurable.Should().BeFalse();
        }

        [Fact]
        public void GivenDurabilityWhenDurableShouldIsDurableBeTrue()
        {
            var durability = Durability.Durable;

            durability.Should().NotBeNull();
            durability.IsDurable.Should().BeTrue();
        }

        [Fact]
        public void GivenDurabilityWhenDurableShouldValueBeDurable()
        {
            var durability = Durability.Durable;

            durability.Should().NotBeNull();
            durability.Value.Should().Be("Durable");
        }

        [Fact]
        public void GivenDurabilityWhenTransientShouldValueBeTransient()
        {
            var durability = Durability.Transient;

            durability.Should().NotBeNull();
            durability.Value.Should().Be("Transient");
        }
    }
}