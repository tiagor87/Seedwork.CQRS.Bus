using System;
using FluentAssertions;
using Seedwork.CQRS.Bus.Core;
using Xunit;

namespace Seedwork.CQRS.Bus.UnitTests
{
    public class ExchangeTests
    {
        [Fact]
        public void GivenExchangesWhenCreateShouldSetDefaults()
        {
            var exchange = Exchange.Create("seedwork", ExchangeType.Direct);

            exchange.Should().NotBeNull();
            exchange.Name.Value.Should().Be("seedwork");
            exchange.Durability.Should().Be(Durability.Transient);
            exchange.Type.Should().Be(ExchangeType.Direct);
            exchange.IsInternal.Should().BeFalse();
            exchange.IsAutoDelete.Should().BeFalse();
        }

        [Fact]
        public void GivenExchangeWhenConfigureAsInternalShouldSetProperty()
        {
            var exchange = Exchange.Create("seedwork", ExchangeType.Direct);
            exchange = exchange.AsInternal();

            exchange.Should().NotBeNull();
            exchange.IsInternal.Should().BeTrue();
        }

        [Fact]
        public void GivenExchangeWhenConfigureAsPublicShouldSetProperty()
        {
            var exchange = Exchange.Create("seedwork", ExchangeType.Direct);
            exchange = exchange.AsPublic();

            exchange.Should().NotBeNull();
            exchange.IsInternal.Should().BeFalse();
        }

        [Fact]
        public void GivenExchangeWhenConfigureDurabilityShouldSetProperty()
        {
            var exchange = Exchange.Create("seedwork", ExchangeType.Direct);
            exchange = exchange.WithDurability(Durability.Durable);

            exchange.Should().NotBeNull();
            exchange.Durability.Should().Be(Durability.Durable);
        }

        [Fact]
        public void GivenExchangeWhenConfigureWithAlternateExchangeShouldAddArgument()
        {
            var exchangeName = ExchangeName.Create("seedwork.core");
            var exchange = Exchange.Create("seedwork", ExchangeType.Direct);
            exchange = exchange.WithAlternateExchange(exchangeName);

            exchange.Should().NotBeNull();
            exchange.Arguments.ContainsKey("alternate-exchange").Should().BeTrue();
            exchange.Arguments["alternate-exchange"].Should().Be(exchangeName.Value);
        }

        [Fact]
        public void GivenExchangeWhenConfigureWithAlternateExchangeTwiceShouldNotThrow()
        {
            var exchangeName = ExchangeName.Create("seedwork.core");
            var exchange = Exchange.Create("seedwork", ExchangeType.Direct);
            Func<Exchange> action = () => exchange.WithAlternateExchange(exchangeName)
                .WithAlternateExchange(exchangeName);

            action.Should().NotThrow();
        }

        [Fact]
        public void GivenExchangeWhenConfigureWithAutoDeleteShouldSetProperty()
        {
            var exchange = Exchange.Create("seedwork", ExchangeType.Direct);
            exchange = exchange.WithAutoDelete();

            exchange.Should().NotBeNull();
            exchange.IsAutoDelete.Should().BeTrue();
        }

        [Fact]
        public void GivenExchangeWhenConfigureWithoutAutoDeleteShouldSetProperty()
        {
            var exchange = Exchange.Create("seedwork", ExchangeType.Direct);
            exchange = exchange.WithoutAutoDelete();

            exchange.Should().NotBeNull();
            exchange.IsAutoDelete.Should().BeFalse();
        }
    }
}