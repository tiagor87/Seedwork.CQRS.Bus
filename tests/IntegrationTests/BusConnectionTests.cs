using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Seedwork.CQRS.Bus.Core;
using Xunit;

namespace Seedwork.CQRS.Bus.IntegrationTests
{
    public class BusConnectionTests : IClassFixture<BusConnectionFixture>
    {
        public BusConnectionTests(BusConnectionFixture connectionFixture)
        {
            _connectionFixture = connectionFixture;
        }

        private readonly BusConnectionFixture _connectionFixture;

        [Fact]
        public async Task GivenConnectionWhenPublishQueueShouldHaveCountOne()
        {
            var exchange = Exchange.Create("seedwork-cqrs-bus.integration-tests", ExchangeType.Direct);
            var queue = Queue.Create($"seedwork-cqrs-bus.integration-tests.queue-{Guid.NewGuid()}")
                .WithAutoDelete();
            var routingKey = RoutingKey.Create(queue.Name.Value);
            const string notification = "Notification message";
            await _connectionFixture.Connection.Publish(exchange, queue, routingKey, notification);

            _connectionFixture.Connection.MessageCount(queue).Should().Be(1);
        }

        [Fact]
        public async Task GivenConnectionWhenSubscribeShouldExecuteCallback()
        {
            var exchange = Exchange.Create("seedwork-cqrs-bus.integration-tests", ExchangeType.Direct);
            var queue = Queue.Create($"seedwork-cqrs-bus.integration-tests.queue-{Guid.NewGuid()}")
                .WithAutoDelete();
            var routingKey = RoutingKey.Create(queue.Name.Value);
            const string notification = "Notification message";
            await _connectionFixture.Connection.Publish(exchange, queue, routingKey, notification);

            IServiceScope callbackScope = null;
            string callbackMessage = null;

            var @event = new AutoResetEvent(false);

            _connectionFixture.Connection.Subscribe<string>(exchange, queue, routingKey, 1, (scope, message) =>
            {
                callbackScope = scope;
                callbackMessage = message;
                @event.Set();
                return Task.CompletedTask;
            });

            @event.WaitOne();

            callbackScope.Should().NotBeNull();
            callbackMessage.Should().Be(notification);
        }
    }
}