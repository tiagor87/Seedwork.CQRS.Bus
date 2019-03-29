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
            var queue = new StubQueue(nameof(Given_Bus_Should_Publish), nameof(Given_Bus_Should_Publish));
            _rabbitMqUtils.Purge(exchange, queue);

            var notification = new StubNotification(exchange, queue.RoutingKey, TimeSpan.Zero,
                nameof(Given_Bus_Should_Publish));
            await _connection.Publish(notification, CancellationToken.None);

            _rabbitMqUtils.MessageCount(exchange, queue).Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Given_Bus_Should_Publish_With_Delay()
        {
            var exchange = StubExchange.Instance;
            var queue = new StubQueue(nameof(Given_Bus_Should_Publish_With_Delay), nameof(Given_Bus_Should_Publish));
            _rabbitMqUtils.Purge(exchange, queue);

            var notification = new StubNotification(exchange, queue.RoutingKey, TimeSpan.FromMilliseconds(500),
                nameof(Given_Bus_Should_Publish_With_Delay));
            await _connection.Publish(notification, CancellationToken.None);

            _rabbitMqUtils.Flush();

            _rabbitMqUtils.MessageCount(exchange, queue).Should().Be(0);

            Thread.Sleep(TimeSpan.FromMilliseconds(500));

            _rabbitMqUtils.Flush();

            _rabbitMqUtils.MessageCount(exchange, queue).Should().BeGreaterThan(0);
        }
    }
}