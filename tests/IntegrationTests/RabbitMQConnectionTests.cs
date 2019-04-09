using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Seedwork.CQRS.Bus.Core;
using Seedwork.CQRS.Bus.IntegrationTests.Stubs;
using Seedwork.CQRS.Bus.IntegrationTests.Utils;
using Xunit;

namespace Seedwork.CQRS.Bus.IntegrationTests
{
    public class RabbitMQConnectionTests : IClassFixture<RabbitMQConnectionFactory>, IClassFixture<RabbitMQUtils>
    {
        public RabbitMQConnectionTests(RabbitMQConnectionFactory factory, RabbitMQUtils rabbitMqUtils)
        {
            _rabbitMqUtils = rabbitMqUtils;
            _connection = factory.Create();
        }

        private readonly RabbitMQUtils _rabbitMqUtils;
        private readonly IBusConnection _connection;

        [Fact]
        public async Task Given_Bus_Should_Publish()
        {
            var exchange = StubExchange.Instance;
            var queue = new StubQueue(
                nameof(Given_Bus_Should_Publish_With_Notification),
                nameof(Given_Bus_Should_Publish_With_Notification));
            _rabbitMqUtils.Purge(exchange, queue);

            var notification = new StubNotification(exchange, queue.RoutingKey, TimeSpan.Zero,
                nameof(Given_Bus_Should_Publish_With_Notification));
            await _connection.Publish(notification.GetExchange(), notification.GetRoutingKey(), notification.GetDelay(),
                notification, CancellationToken.None);

            _rabbitMqUtils.MessageCount(exchange, queue).Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Given_Bus_Should_Publish_Notification_With_Delay()
        {
            var exchange = StubExchange.Instance;
            var queue = new StubQueue(nameof(Given_Bus_Should_Publish_Notification_With_Delay),
                nameof(Given_Bus_Should_Publish_Notification_With_Delay));
            _rabbitMqUtils.Purge(exchange, queue);

            var notification = new StubNotification(exchange, queue.RoutingKey, TimeSpan.FromMilliseconds(500),
                nameof(Given_Bus_Should_Publish_Notification_With_Delay));
            await _connection.Publish(notification, CancellationToken.None);

            _rabbitMqUtils.Flush();

            _rabbitMqUtils.MessageCount(exchange, queue).Should().Be(0);

            Thread.Sleep(TimeSpan.FromMilliseconds(500));

            _rabbitMqUtils.Flush();

            _rabbitMqUtils.MessageCount(exchange, queue).Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Given_Bus_Should_Publish_With_Delay()
        {
            var exchange = StubExchange.Instance;
            var queue = new StubQueue(nameof(Given_Bus_Should_Publish_With_Delay),
                nameof(Given_Bus_Should_Publish_With_Delay));
            _rabbitMqUtils.Purge(exchange, queue);

            var notification = new StubNotification(exchange, queue.RoutingKey, TimeSpan.FromMilliseconds(500),
                nameof(Given_Bus_Should_Publish_With_Delay));
            await _connection.Publish(notification.GetExchange(), notification.GetRoutingKey(), notification.GetDelay(),
                notification, CancellationToken.None);

            _rabbitMqUtils.Flush();

            _rabbitMqUtils.MessageCount(exchange, queue).Should().Be(0);

            Thread.Sleep(TimeSpan.FromMilliseconds(500));

            _rabbitMqUtils.Flush();

            _rabbitMqUtils.MessageCount(exchange, queue).Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Given_Bus_Should_Publish_With_Notification()
        {
            var exchange = StubExchange.Instance;
            var queue = new StubQueue(
                nameof(Given_Bus_Should_Publish_With_Notification),
                nameof(Given_Bus_Should_Publish_With_Notification));
            _rabbitMqUtils.Purge(exchange, queue);

            var notification = new StubNotification(exchange, queue.RoutingKey, TimeSpan.Zero,
                nameof(Given_Bus_Should_Publish_With_Notification));
            await _connection.Publish(notification, CancellationToken.None);

            _rabbitMqUtils.MessageCount(exchange, queue).Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Given_observer_should_notify()
        {
            var exchange = StubExchange.Instance;
            var queue = new StubQueue(nameof(Given_observer_should_notify), nameof(Given_observer_should_notify));
            _rabbitMqUtils.Purge(exchange, queue);

            var observer = new StubObserver(queue);
            await _connection.Subscribe(observer.GetExchange(), observer.GetQueue(), observer);

            _rabbitMqUtils.Publish(exchange, queue.RoutingKey,
                new StubNotification(nameof(BusObserverTests)));

            _rabbitMqUtils.Flush();

            observer.Value.Should().NotBeNull();
            observer.Value.Message.Should().Be(nameof(BusObserverTests));
        }
    }
}