using System;
using FluentAssertions;
using Seedwork.CQRS.Bus.Core;
using Xunit;

namespace Seedwork.CQRS.Bus.UnitTests
{
    public class RoutingKeyTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void GivenRoutingKeyWhenEmptyShouldThrowsArgumentNullException(string routing)
        {
            Func<RoutingKey> action = () => RoutingKey.Create(routing);
            action.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData("seedwork.")]
        [InlineData(".seedwork.cqrs")]
        [InlineData(".seedwork..cqrs")]
        [InlineData("seedwork.cqrs--bus")]
        [InlineData("seedwork.cqrs-bus.")]
        public void GivenRoutingKeyWhenInvalidShouldThrowsArgumentException(string routing)
        {
            Func<RoutingKey> action = () => RoutingKey.Create(routing);
            action.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("seedwork.*")]
        [InlineData("seedwork.cqrs")]
        [InlineData("seedwork.cqrs-bus")]
        [InlineData("seedwork.cqrs-bus.core")]
        [InlineData("seedwork.cqrs-bus.#")]
        [InlineData("seedwork.#.core")]
        [InlineData("seedwork.*.core")]
        public void GivenRoutingKeyShouldCreate(string routing)
        {
            var routingKey = RoutingKey.Create(routing);

            routingKey.Should().NotBeNull();
            routingKey.Value.Should().Be(routing);
        }

        [Theory]
        [InlineData(" seedwork.cqrs ", "seedwork.cqrs")]
        [InlineData(" seedwork.cqrs-bus", "seedwork.cqrs-bus")]
        [InlineData("seedwork.cqrs-bus.core ", "seedwork.cqrs-bus.core")]
        public void GivenRoutingKeyWhenSpaceAroundShouldTrim(string routing, string expected)
        {
            var routingKey = RoutingKey.Create(routing);

            routingKey.Should().NotBeNull();
            routingKey.Value.Should().Be(expected);
        }

        [Fact]
        public void GivenRoutingKeysWhenValuesAreSameShouldBeEquals()
        {
            var routingKey1 = RoutingKey.Create("seedwork.cqrs");
            var routingKey2 = RoutingKey.Create("seedwork.cqrs");

            routingKey1.Should().Be(routingKey2);
        }
    }
}