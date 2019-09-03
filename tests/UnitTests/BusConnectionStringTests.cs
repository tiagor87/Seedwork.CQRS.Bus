using System;
using FluentAssertions;
using Seedwork.CQRS.Bus.Core;
using Xunit;

namespace Seedwork.CQRS.Bus.UnitTests
{
    public class BusConnectionStringTests
    {
        [Fact]
        public void GivenBusConnectionStringWhenInvalidShouldThrow()
        {
            Func<BusConnectionString> action = () => BusConnectionString.Create("aaaa");

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GivenBusConnectionStringWhenUriIsValidShouldCreate()
        {
            var connectionString = BusConnectionString.Create("amqp://guest:guest@localhost/");

            connectionString.Should().NotBeNull();
        }
    }
}