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
    public class NotificationHandlerTests : IClassFixture<RabbitMQUtils>, IClassFixture<RabbitMQConnectionFactory>
    {
        public NotificationHandlerTests(RabbitMQUtils rabbitMqUtils, RabbitMQConnectionFactory factory)
        {
            _rabbitMqUtils = rabbitMqUtils ?? throw new ArgumentNullException(nameof(rabbitMqUtils));
            _connection = factory?.Create() ?? throw new ArgumentNullException(nameof(factory));
            ;
        }

        private readonly RabbitMQUtils _rabbitMqUtils;
        private readonly IBusConnection _connection;

        [Fact]
        public async Task Given_notification_handler_should_publish_message()
        {
            var exchange = StubExchange.Instance;
            var queue = new StubQueue(nameof(Given_notification_handler_should_publish_message),
                nameof(Given_notification_handler_should_publish_message));
            var notification = new StubNotification(exchange, queue.RoutingKey, TimeSpan.Zero, "Test message");

            var observer = new StubObserver(queue);
            await _connection.Subscribe(observer);
            var handler = new NotificationHandler(_connection);
            await handler.Handle(notification, CancellationToken.None);

            _rabbitMqUtils.Flush();

            observer.Error.Should().BeNull();
            observer.Value.Should().NotBeNull();
            observer.Value.Message.Should().Be(notification.Message);
        }
    }
}