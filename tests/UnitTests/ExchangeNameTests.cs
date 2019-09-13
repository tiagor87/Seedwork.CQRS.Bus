using System;
using FluentAssertions;
using Seedwork.CQRS.Bus.Core;
using Xunit;

namespace Seedwork.CQRS.Bus.UnitTests
{
    public class ExchangeNameTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void GivenNameWhenEmptyShouldThrowsArgumentNullException(string name)
        {
            Func<ExchangeName> action = () => ExchangeName.Create(name);
            action.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData("seedwork")]
        [InlineData("seedwork.cqrs")]
        [InlineData("seedwork.cqrs-bus")]
        [InlineData("seedwork.cqrs-bus.core")]
        public void GivenNameShouldCreate(string name)
        {
            var exchangeName = ExchangeName.Create(name);

            exchangeName.Should().NotBeNull();
            exchangeName.Value.Should().Be(name);
        }

        [Theory]
        [InlineData(" seedwork.cqrs ", "seedwork.cqrs")]
        [InlineData(" seedwork.cqrs-bus", "seedwork.cqrs-bus")]
        [InlineData("seedwork.cqrs-bus.core ", "seedwork.cqrs-bus.core")]
        public void GivenNameWhenSpaceAroundShouldTrim(string name, string expected)
        {
            var exchangeName = ExchangeName.Create(name);

            exchangeName.Should().NotBeNull();
            exchangeName.Value.Should().Be(expected);
        }

        [Fact]
        public void GivenNamesWhenValuesAreSameShouldBeEquals()
        {
            var exchangeName1 = ExchangeName.Create("seedwork.cqrs");
            var exchangeName2 = ExchangeName.Create("seedwork.cqrs");

            exchangeName1.Should().Be(exchangeName2);
        }
    }
}