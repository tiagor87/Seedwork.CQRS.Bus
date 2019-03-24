using System;
using System.Threading.Tasks;
using FluentAssertions;
using Seedwork.CQRS.Bus.Core;
using Seedwork.CQRS.Bus.IntegrationTests.Stubs;
using Seedwork.CQRS.Bus.IntegrationTests.Utils;
using Xunit;

namespace Seedwork.CQRS.Bus.IntegrationTests
{
    public class BusObserverTests : IClassFixture<RabbitMQUtils>, IClassFixture<RabbitMQConnectionFactory>
    {
        public BusObserverTests(RabbitMQUtils rabbitMqUtils, RabbitMQConnectionFactory factory)
        {
            _rabbitMqUtils = rabbitMqUtils ?? throw new ArgumentNullException(nameof(rabbitMqUtils));
            _connection = factory?.Create() ?? throw new ArgumentNullException(nameof(factory));
        }

        private readonly RabbitMQUtils _rabbitMqUtils;
        private readonly IBusConnection _connection;

        [Fact]
        public async Task Given_observer_should_notify()
        {
            var queue = new StubQueue("SuccessQueue", "Seedwork.CQRS.Bus.Success");
            _rabbitMqUtils.Purge(queue);

            var observer = new StubObserver(queue);
            await _connection.Subscribe(observer);

            _rabbitMqUtils.Publish(StubExchange.Instance, queue.RoutingKey,
                new StubNotification(nameof(BusObserverTests)));

            _rabbitMqUtils.Flush();

            observer.Value.Should().NotBeNull();
            observer.Value.Message.Should().Be(nameof(BusObserverTests));
        }

        [Fact]
        public async Task Given_observer_when_dispose_should_complete()
        {
            var observer = new StubObserver();

            await _connection.Subscribe(observer);

            observer.Dispose();
            observer.Completed.Should().BeTrue();
        }

        [Fact]
        public async Task Given_observer_when_success_should_ack()
        {
            var queue = new StubQueue("AckSuccessQueue", "Seedwork.CQRS.Bus.AckSuccess");
            _rabbitMqUtils.Purge(queue);

            var observer = new StubObserver(queue);
            await _connection.Subscribe(observer);

            _rabbitMqUtils.Publish(StubExchange.Instance, queue.RoutingKey,
                new StubNotification(nameof(BusObserverTests)));

            _rabbitMqUtils.Flush();

            observer.Value.Should().NotBeNull();
            observer.Value.Message.Should().Be(nameof(BusObserverTests));

            _rabbitMqUtils.MessageCount(queue).Should().Be(0);
        }

        [Fact]
        public async Task Given_observer_when_throw_should_notify_error()
        {
            var queue = new StubQueue("ErrorQueue", "Seedwork.CQRS.Bus.ErrorQueue");
            _rabbitMqUtils.Purge(queue);

            var observer = new StubObserver(queue);
            await _connection.Subscribe(observer);

            _rabbitMqUtils.Publish(StubExchange.Instance, queue.RoutingKey, 10);

            _rabbitMqUtils.Flush();

            observer.Error.Should().NotBeNull();
        }

        [Fact]
        public async Task Given_observer_when_throw_should_requeue()
        {
            var queue = new StubQueue("ErrorQueue", "Seedwork.CQRS.Bus.ErrorQueue");
            _rabbitMqUtils.Purge(queue);

            var observer = new StubObserver(queue);
            await _connection.Subscribe(observer);

            _rabbitMqUtils.Publish(StubExchange.Instance, queue.RoutingKey, 10);

            _rabbitMqUtils.Flush();

            observer.Error.Should().NotBeNull();
            observer.Dispose();

            observer = new StubObserver(queue);
            await _connection.Subscribe(observer);

            _rabbitMqUtils.Flush();

            observer.Error.Should().NotBeNull();
        }
    }
}