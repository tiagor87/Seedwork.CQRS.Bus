using System;
using FluentAssertions;
using Xunit;

namespace Seedwork.CQRS.Bus.Core.Tests.UnitTests
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
        
        [Fact]
        public void GivenBusConnectionStringWhenIgnoreCertificateErrorsShouldCreate()
        {
            var connectionString = BusConnectionString.Create("amqp://guest:guest@localhost/", false);

            connectionString.Should().NotBeNull();
            connectionString.ValidateCertificate.Should().BeFalse();
        }

        [Fact]
        public void GivenConnectionStringsWhenValuesAreSameShouldBeEquals()
        {
            var connectionString1 = BusConnectionString.Create("amqp://guest:guest@localhost/");
            var connectionString2 = BusConnectionString.Create("amqp://guest:guest@localhost/");

            connectionString1.Should().Be(connectionString2);
        }
    }
}