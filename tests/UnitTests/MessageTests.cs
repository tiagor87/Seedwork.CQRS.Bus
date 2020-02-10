using System;
using FluentAssertions;
using Seedwork.CQRS.Bus.Core;
using Xunit;

namespace Seedwork.CQRS.Bus.Tests.UnitTests
{
    public class MessageTests
    {
        /*
        [Fact]
        public void GivenMessageShouldCreate()
        {
            var message = ConsumerMessage.Create("message", 10);

            message.Should().NotBeNull();
            message.Data.Should().Be("message");
            message.AttemptCount.Should().Be(0);
            message.MaxAttempts.Should().Be(10);
        }

        [Fact]
        public void GivenMessageWhenDataNullShouldThrow()
        {
            Func<ConsumerMessage> action = () => ConsumerMessage.Create(null, 10);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GivenMessageWhenMaxAttemptLessThanZeroShouldThrow()
        {
            Func<ConsumerMessage> action = () => ConsumerMessage.Create("message", -1);

            action.Should().Throw<ArgumentException>();
        }
        */
    }
}