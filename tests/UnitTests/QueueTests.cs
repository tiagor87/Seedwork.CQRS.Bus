using System;
using FluentAssertions;
using Seedwork.CQRS.Bus.Core;
using Xunit;

namespace Seedwork.CQRS.Bus.Tests.UnitTests
{
    public class QueueTests
    {
        [Fact]
        public void GivenQueueWhenConfigureDurabilityShouldSetProperty()
        {
            var queue = Queue.Create("seedwork.cqrs");
            queue = queue.WithDurability(Durability.Durable);

            queue.Should().NotBeNull();
            queue.Durability.Should().Be(Durability.Durable);
        }

        [Fact]
        public void GivenQueueWhenConfigureExpiresInShouldAddArgument()
        {
            var ttl = TimeSpan.FromMinutes(5);
            var queue = Queue.Create("seedwork.cqrs");
            queue = queue.ExpiresIn(ttl);

            queue.Should().NotBeNull();
            queue.Arguments.ContainsKey("x-expires").Should().BeTrue();
            queue.Arguments["x-expires"].Should().Be(ttl.TotalMilliseconds);
        }

        [Fact]
        public void GivenQueueWhenConfigureExpiresInTwiceShouldNotThrow()
        {
            var ttl = TimeSpan.FromMinutes(5);
            var queue = Queue.Create("seedwork.cqrs");
            Func<Queue> action = () => queue.ExpiresIn(ttl).ExpiresIn(ttl);

            action.Should().NotThrow();
        }

        [Fact]
        public void GivenQueueWhenConfigureMaxMessageSizeShouldAddArgument()
        {
            var queue = Queue.Create("seedwork.cqrs");
            queue = queue.WithMaxTotalMessagesSizeInBytes(1000);

            queue.Should().NotBeNull();
            queue.Arguments.ContainsKey("x-max-length-bytes").Should().BeTrue();
            queue.Arguments["x-max-length-bytes"].Should().Be(1000);
        }

        [Fact]
        public void GivenQueueWhenConfigureMaxMessageSizeTwiceShouldNotThrow()
        {
            var queue = Queue.Create("seedwork.cqrs");
            Func<Queue> action = () => queue.WithMaxTotalMessagesSizeInBytes(1000)
                .WithMaxTotalMessagesSizeInBytes(1000);

            action.Should().NotThrow();
        }

        [Fact]
        public void GivenQueueWhenConfigureMaxPriorityShouldAddArgument()
        {
            var queue = Queue.Create("seedwork.cqrs");
            queue = queue.WithMaxPriority(10);

            queue.Should().NotBeNull();
            queue.Arguments.ContainsKey("x-max-priority").Should().BeTrue();
            queue.Arguments["x-max-priority"].Should().Be(10);
        }

        [Fact]
        public void GivenQueueWhenConfigureMaxPriorityTwiceShouldNotThrow()
        {
            var queue = Queue.Create("seedwork.cqrs");
            Func<Queue> action = () => queue.WithMaxPriority(10)
                .WithMaxPriority(10);

            action.Should().NotThrow();
        }

        [Fact]
        public void GivenQueueWhenConfigureMaxReadyMessagesToDropHeadShouldAddArgument()
        {
            var queue = Queue.Create("seedwork.cqrs");
            queue = queue.WithMaxReadyMessages(20, OverflowMessagesBehavior.DropHead);

            queue.Should().NotBeNull();
            queue.Arguments.ContainsKey("x-max-length").Should().BeTrue();
            queue.Arguments["x-max-length"].Should().Be(20);
            queue.Arguments.ContainsKey("x-overflow").Should().BeTrue();
            queue.Arguments["x-overflow"].Should().Be(OverflowMessagesBehavior.DropHead.Value);
        }

        [Fact]
        public void GivenQueueWhenConfigureMaxReadyMessagesToRejectPublishShouldAddArgument()
        {
            var queue = Queue.Create("seedwork.cqrs");
            queue = queue.WithMaxReadyMessages(10, OverflowMessagesBehavior.RejectPublish);

            queue.Should().NotBeNull();
            queue.Arguments.ContainsKey("x-max-length").Should().BeTrue();
            queue.Arguments["x-max-length"].Should().Be(10);
            queue.Arguments.ContainsKey("x-overflow").Should().BeTrue();
            queue.Arguments["x-overflow"].Should().Be(OverflowMessagesBehavior.RejectPublish.Value);
        }

        [Fact]
        public void GivenQueueWhenConfigureMaxReadyMessagesTwiceShouldNotThrow()
        {
            var queue = Queue.Create("seedwork.cqrs");
            Func<Queue> action = () => queue.WithMaxReadyMessages(20, OverflowMessagesBehavior.DropHead)
                .WithMaxReadyMessages(20, OverflowMessagesBehavior.DropHead);

            action.Should().NotThrow();
        }

        [Fact]
        public void GivenQueueWhenConfigureMessageExpiresInShouldAddArgument()
        {
            var ttl = TimeSpan.FromMinutes(5);
            var queue = Queue.Create("seedwork.cqrs");
            queue = queue.MessagesExpiresIn(ttl);

            queue.Should().NotBeNull();
            queue.Arguments.ContainsKey("x-message-ttl").Should().BeTrue();
            queue.Arguments["x-message-ttl"].Should().Be(ttl.TotalMilliseconds);
        }

        [Fact]
        public void GivenQueueWhenConfigureMessageExpiresInTwiceShouldNotThrow()
        {
            var ttl = TimeSpan.FromMinutes(5);
            var queue = Queue.Create("seedwork.cqrs");
            Func<Queue> action = () => queue.MessagesExpiresIn(ttl).MessagesExpiresIn(ttl);

            action.Should().NotThrow();
        }

        [Fact]
        public void GivenQueueWhenConfigureQueueModeLazyShouldAddArgumentLazy()
        {
            var queue = Queue.Create("seedwork.cqrs");
            queue = queue.WithMode(QueueMode.Lazy);

            queue.Should().NotBeNull();
            queue.Arguments.ContainsKey("x-queue-mode").Should().BeTrue();
            queue.Arguments["x-queue-mode"].Should().Be(QueueMode.Lazy.Value);
        }

        [Fact]
        public void GivenQueueWhenConfigureQueueModeNormalShouldAddArgumentEmpty()
        {
            var queue = Queue.Create("seedwork.cqrs");
            queue = queue.WithMode(QueueMode.Normal);

            queue.Should().NotBeNull();
            queue.Arguments.ContainsKey("x-queue-mode").Should().BeTrue();
            queue.Arguments["x-queue-mode"].Should().Be(QueueMode.Normal.Value);
        }

        [Fact]
        public void GivenQueueWhenConfigureQueueModeTwiceShouldNotThrow()
        {
            var queue = Queue.Create("seedwork.cqrs");
            Func<Queue> action = () => queue.WithMode(QueueMode.Normal)
                .WithMode(QueueMode.Normal);

            action.Should().NotThrow();
        }

        [Fact]
        public void GivenQueueWhenConfigureToSendExpiredMessagesToShouldAddArgument()
        {
            var exchangeName = ExchangeName.Create("seedwork");
            var routingKey = RoutingKey.Create("seedwork.cqrs");
            var queue = Queue.Create("seedwork.cqrs");
            queue = queue.SendExpiredMessagesTo(exchangeName, routingKey);

            queue.Should().NotBeNull();
            queue.Arguments.ContainsKey("x-dead-letter-exchange").Should().BeTrue();
            queue.Arguments.ContainsKey("x-dead-letter-routing-key").Should().BeTrue();
            queue.Arguments["x-dead-letter-exchange"].Should().Be(exchangeName.Value);
            queue.Arguments["x-dead-letter-routing-key"].Should().Be(routingKey.Value);
        }

        [Fact]
        public void GivenQueueWhenConfigureToSendExpiredMessagesToTwiceShouldNotThrow()
        {
            var exchangeName = ExchangeName.Create("seedwork");
            var routingKey = RoutingKey.Create("seedwork.cqrs");
            var queue = Queue.Create("seedwork.cqrs");
            Func<Queue> action = () => queue.SendExpiredMessagesTo(exchangeName, routingKey)
                .SendExpiredMessagesTo(exchangeName, routingKey);

            action.Should().NotThrow();
        }

        [Fact]
        public void GivenQueueWhenConfigureWithAutoDeleteShouldSetProperty()
        {
            var queue = Queue.Create("seedwork.cqrs");
            queue = queue.WithAutoDelete();

            queue.Should().NotBeNull();
            queue.IsAutoDelete.Should().BeTrue();
        }

        [Fact]
        public void GivenQueueWhenConfigureWithoutAutoDeleteShouldSetProperty()
        {
            var queue = Queue.Create("seedwork.cqrs");
            queue = queue.WithoutAutoDelete();

            queue.Should().NotBeNull();
            queue.IsAutoDelete.Should().BeFalse();
        }

        [Fact]
        public void GivenQueueWhenCreateShouldSetDefaultValues()
        {
            var queue = Queue.Create("seedwork.cqrs");

            queue.Should().NotBeNull();
            queue.Name.Value.Should().Be("seedwork.cqrs");
            queue.Durability.Should().Be(Durability.Transient);
            queue.IsAutoDelete.Should().BeFalse();
        }
    }
}