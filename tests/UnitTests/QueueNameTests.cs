using System;
using FluentAssertions;
using Seedwork.CQRS.Bus.Core;
using Xunit;

namespace Seedwork.CQRS.Bus.UnitTests
{
    public class QueueNameTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void GivenNameWhenEmptyShouldThrowsArgumentNullException(string name)
        {
            Func<QueueName> action = () => QueueName.Create(name);
            action.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData("seedwork.cqrs")]
        [InlineData("seedwork.cqrs-bus")]
        [InlineData("seedwork.cqrs-bus.core")]
        public void GivenNameShouldCreate(string name)
        {
            var queueName = QueueName.Create(name);

            queueName.Should().NotBeNull();
            queueName.Value.Should().Be(name);
        }

        [Theory]
        [InlineData(" seedwork.cqrs ", "seedwork.cqrs")]
        [InlineData(" seedwork.cqrs-bus", "seedwork.cqrs-bus")]
        [InlineData("seedwork.cqrs-bus.core ", "seedwork.cqrs-bus.core")]
        public void GivenNameWhenSpaceAroundShouldTrim(string name, string expected)
        {
            var queueName = QueueName.Create(name);

            queueName.Should().NotBeNull();
            queueName.Value.Should().Be(expected);
        }

        [Fact]
        public void GivenNamesWhenValuesAreSameShouldBeEquals()
        {
            var queueName1 = QueueName.Create("seedwork.cqrs");
            var queueName2 = QueueName.Create("seedwork.cqrs");

            queueName1.Should().Be(queueName2);
        }
    }
}