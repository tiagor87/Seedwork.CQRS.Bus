using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Seedwork.CQRS.Bus.Core;
using Xunit;
using ExchangeType = Seedwork.CQRS.Bus.Core.ExchangeType;

namespace Seedwork.CQRS.Bus.UnitTests
{
    public class BusConnectionTests
    {
        public BusConnectionTests()
        {
            _connectionFactoryMock = new Mock<IConnectionFactory>();
            _busSerializerMock = new Mock<IBusSerializer>();
            _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            _connectionMock = new Mock<IConnection>();
            _channelMock = new Mock<IModel>();
            _scopeMock = new Mock<IServiceScope>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _basicPropertiesMock = new Mock<IBasicProperties>();
            _headersMock = new Mock<IDictionary<string, object>>();
            _basicPropertiesMock.SetupGet(x => x.Headers)
                .Returns(_headersMock.Object)
                .Verifiable();
            _scopeMock.SetupGet(x => x.ServiceProvider)
                .Returns(_serviceProviderMock.Object)
                .Verifiable();
            _connectionFactoryMock.Setup(x => x.CreateConnection())
                .Returns(_connectionMock.Object)
                .Verifiable();
            _connectionMock.Setup(x => x.CreateModel())
                .Returns(_channelMock.Object)
                .Verifiable();
            _busConnection = new BusConnection(
                _connectionFactoryMock.Object,
                _busSerializerMock.Object,
                _serviceScopeFactoryMock.Object);
            _serviceScopeFactoryMock.Setup(x => x.CreateScope())
                .Returns(_scopeMock.Object)
                .Verifiable();
            _channelMock.Setup(x => x.CreateBasicProperties())
                .Returns(_basicPropertiesMock.Object)
                .Verifiable();
        }

        private readonly Mock<IConnectionFactory> _connectionFactoryMock;
        private readonly Mock<IBusSerializer> _busSerializerMock;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
        private readonly Mock<IModel> _channelMock;
        private readonly Mock<IConnection> _connectionMock;
        private readonly BusConnection _busConnection;
        private readonly Mock<IServiceScope> _scopeMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IBasicProperties> _basicPropertiesMock;
        private readonly Mock<IDictionary<string, object>> _headersMock;

        [Fact]
        public void GivenConnectionWhenCreateShouldNotTryToConnect()
        {
            var busConnection = new BusConnection(
                _connectionFactoryMock.Object,
                _busSerializerMock.Object,
                _serviceScopeFactoryMock.Object);

            busConnection.Should().NotBeNull();
            _connectionFactoryMock.Verify(x => x.CreateConnection(), Times.Never());
        }

        [Fact]
        public async Task
            GivenConnectionWhenPublishShouldConfigureBasicPropertiesForRetry()
        {
            const string notification = "test";
            var exchange = Exchange.Create("test", ExchangeType.Direct);
            var queue = Queue.Create("test.requested");
            var routingKey = RoutingKey.Create("test.route");
            var body = Encoding.UTF8.GetBytes("test");

            var headersMock = new Mock<IDictionary<string, object>>();

            _busSerializerMock.Setup(x => x.Serialize(It.IsAny<object>()))
                .ReturnsAsync(body)
                .Verifiable();
            _basicPropertiesMock.Setup(x => x.Headers)
                .Returns(headersMock.Object)
                .Verifiable();

            await _busConnection.Publish(exchange, queue, routingKey, notification);

            _channelMock.Verify(x => x.CreateBasicProperties(), Times.Once());
            _basicPropertiesMock.VerifySet(x => x.Headers = new Dictionary<string, object>());
            headersMock.Verify(x => x.Add(nameof(Message.MaxAttempts), It.IsAny<object>()));
            headersMock.Verify(x => x.Add(nameof(Message.AttemptCount), It.IsAny<object>()));
        }

        [Fact]
        public async Task GivenConnectionWhenPublishShouldDeclareExchangePublishMessageCloseAndDisposeChannel()
        {
            const string notification = "test";
            var exchange = Exchange.Create("test", ExchangeType.Direct);
            var routingKey = RoutingKey.Create("test.route");
            var body = Encoding.UTF8.GetBytes("test");
            _busSerializerMock.Setup(x => x.Serialize(It.IsAny<object>()))
                .ReturnsAsync(body)
                .Verifiable();

            await _busConnection.Publish(exchange, routingKey, notification);

            _connectionFactoryMock.Verify(x => x.CreateConnection(), Times.Once());
            _connectionMock.Verify(x => x.CreateModel(), Times.Once());
            _channelMock.Verify(x => x.ExchangeDeclare(
                exchange.Name.Value,
                exchange.Type.Value,
                exchange.Durability.IsDurable,
                exchange.IsAutoDelete,
                It.IsAny<IDictionary<string, object>>()), Times.Once());
            _channelMock.Verify(
                x => x.BasicPublish(
                    exchange.Name.Value,
                    routingKey.Value,
                    false,
                    _basicPropertiesMock.Object,
                    body), Times.Once());
            _channelMock.Verify(x => x.Close(), Times.Once());
            _channelMock.Verify(x => x.Dispose(), Times.Once());
        }

        [Fact]
        public async Task
            GivenConnectionWhenPublishShouldDeclareExchangeQueueAndBindRoutingKey()
        {
            const string notification = "test";
            var exchange = Exchange.Create("test", ExchangeType.Direct);
            var queue = Queue.Create("test.requested");
            var routingKey = RoutingKey.Create("test.route");
            var body = Encoding.UTF8.GetBytes("test");

            var headersMock = new Mock<IDictionary<string, object>>();

            _busSerializerMock.Setup(x => x.Serialize(It.IsAny<object>()))
                .ReturnsAsync(body)
                .Verifiable();
            _basicPropertiesMock.Setup(x => x.Headers)
                .Returns(headersMock.Object)
                .Verifiable();

            await _busConnection.Publish(exchange, queue, routingKey, notification);

            _connectionFactoryMock.Verify(x => x.CreateConnection(), Times.Once());
            _connectionMock.Verify(x => x.CreateModel(), Times.Once());
            _channelMock.Verify(x => x.ExchangeDeclare(
                exchange.Name.Value,
                exchange.Type.Value,
                exchange.Durability.IsDurable,
                exchange.IsAutoDelete,
                It.IsAny<IDictionary<string, object>>()), Times.Once());
            _channelMock.Verify(x => x.QueueDeclare(
                queue.Name.Value,
                queue.Durability.IsDurable,
                false,
                queue.IsAutoDelete,
                It.IsAny<IDictionary<string, object>>()));
            _channelMock.Verify(x => x.QueueBind(
                queue.Name.Value,
                exchange.Name.Value,
                routingKey.Value,
                It.IsAny<IDictionary<string, object>>()));
        }

        [Fact]
        public async Task
            GivenConnectionWhenPublishShouldPublishMessageAndCloseAndDisposeChannel()
        {
            const string notification = "test";
            var exchange = Exchange.Create("test", ExchangeType.Direct);
            var queue = Queue.Create("test.requested");
            var routingKey = RoutingKey.Create("test.route");
            var body = Encoding.UTF8.GetBytes("test");

            var headersMock = new Mock<IDictionary<string, object>>();

            _busSerializerMock.Setup(x => x.Serialize(It.IsAny<object>()))
                .ReturnsAsync(body)
                .Verifiable();
            _basicPropertiesMock.Setup(x => x.Headers)
                .Returns(headersMock.Object)
                .Verifiable();

            await _busConnection.Publish(exchange, queue, routingKey, notification);

            _channelMock.Verify(
                x => x.BasicPublish(
                    exchange.Name.Value,
                    routingKey.Value,
                    false,
                    _basicPropertiesMock.Object,
                    body), Times.Once());
            _channelMock.Verify(x => x.Close(), Times.Once());
            _channelMock.Verify(x => x.Dispose(), Times.Once());
        }

        [Fact]
        public void GivenConnectionWhenSubscribeShouldExecuteCallbackAndAckOnSuccess()
        {
            var exchange = Exchange.Create("test", ExchangeType.Direct);
            var queue = Queue.Create("test.requested");
            var routingKey = RoutingKey.Create("test.route");
            var body = Encoding.UTF8.GetBytes("test");
            const ushort deliveryTag = 1;

            _busSerializerMock.Setup(x => x.Deserialize<string>(body))
                .ReturnsAsync("test")
                .Verifiable();

            _channelMock.Setup(x => x.BasicConsume(
                    queue.Name.Value,
                    false,
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<IDictionary<string, object>>(),
                    It.IsAny<IBasicConsumer>()))
                .Callback((string queueName, bool autoAck, string consumerTag, bool noLocal, bool exclusive,
                    IDictionary<string, object> _, IBasicConsumer consumer) =>
                {
                    ((AsyncEventingBasicConsumer) consumer).HandleBasicDeliver(
                        consumerTag,
                        deliveryTag,
                        false,
                        exchange.Name.Value,
                        routingKey.Value,
                        _basicPropertiesMock.Object,
                        body).Wait();
                })
                .Returns(Guid.NewGuid().ToString());

            var isExecuted = false;
            var autoResetEvent = new AutoResetEvent(false);
            _busConnection.Subscribe<string>(exchange, queue, routingKey, 10, (scope, @event) =>
            {
                isExecuted = true;
                autoResetEvent.Set();
                return Task.CompletedTask;
            });

            autoResetEvent.WaitOne();

            isExecuted.Should().BeTrue();
            _channelMock.Verify(x => x.BasicQos(0, 10, false), Times.Once());
            _channelMock.Verify(x => x.BasicConsume(
                queue.Name.Value,
                false,
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<IBasicConsumer>()), Times.Once());
            _channelMock.Verify(x => x.BasicAck(deliveryTag, false), Times.Once());
        }

        [Fact]
        public void GivenConnectionWhenSubscribeShouldExecuteCallbackAndNackOnFailure()
        {
            var exchange = Exchange.Create("test", ExchangeType.Direct);
            var queue = Queue.Create("test.requested");
            var routingKey = RoutingKey.Create("test.route");
            var body = Encoding.UTF8.GetBytes("test");
            const ushort deliveryTag = 1;

            _busSerializerMock.Setup(x => x.Deserialize<string>(body))
                .ReturnsAsync("test")
                .Verifiable();

            var autoResetEvent = new AutoResetEvent(false);

            _channelMock.Setup(x => x.BasicConsume(
                    queue.Name.Value,
                    false,
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<IDictionary<string, object>>(),
                    It.IsAny<IBasicConsumer>()))
                .Callback((string queueName, bool autoAck, string consumerTag, bool noLocal, bool exclusive,
                    IDictionary<string, object> _, IBasicConsumer consumer) =>
                {
                    ((AsyncEventingBasicConsumer) consumer).HandleBasicDeliver(
                        consumerTag,
                        deliveryTag,
                        false,
                        exchange.Name.Value,
                        routingKey.Value,
                        _basicPropertiesMock.Object,
                        body).Wait();
                })
                .Returns(Guid.NewGuid().ToString());

            _channelMock.Setup(x => x.BasicNack(deliveryTag, false, false))
                .Callback((ulong tag, bool multiple, bool requeue) => autoResetEvent.Set())
                .Verifiable();

            _busConnection.Subscribe<string>(
                exchange,
                queue,
                routingKey,
                10,
                (scope, @event) => throw new Exception());

            autoResetEvent.WaitOne();

            _channelMock.Verify(x => x.BasicNack(deliveryTag, false, false), Times.Once());
        }

        [Fact]
        public void GivenConnectionWhenSubscribeShouldExecuteCallbackAndRetryOnFailure()
        {
            var exchange = Exchange.Create("test", ExchangeType.Direct);
            var queue = Queue.Create("test.requested");
            var routingKey = RoutingKey.Create("test.route");
            var body = Encoding.UTF8.GetBytes("test");
            const ushort deliveryTag = 1;

            var loggerMock = new Mock<IBusLogger>();

            _busSerializerMock.Setup(x => x.Deserialize<string>(body))
                .ReturnsAsync("test")
                .Verifiable();
            _serviceProviderMock.Setup(x => x.GetService(typeof(IBusLogger)))
                .Returns(loggerMock.Object)
                .Verifiable();

            var autoResetEvent = new AutoResetEvent(false);

            _channelMock.Setup(x => x.BasicConsume(
                    queue.Name.Value,
                    false,
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<IDictionary<string, object>>(),
                    It.IsAny<IBasicConsumer>()))
                .Callback((string queueName, bool autoAck, string consumerTag, bool noLocal, bool exclusive,
                    IDictionary<string, object> _, IBasicConsumer consumer) =>
                {
                    ((AsyncEventingBasicConsumer) consumer).HandleBasicDeliver(
                        consumerTag,
                        deliveryTag,
                        false,
                        exchange.Name.Value,
                        routingKey.Value,
                        _basicPropertiesMock.Object,
                        body).Wait();
                })
                .Returns(Guid.NewGuid().ToString());

            _channelMock.Setup(x => x.BasicNack(deliveryTag, false, false))
                .Callback((ulong tag, bool multiple, bool requeue) => autoResetEvent.Set())
                .Verifiable();

            _busConnection.Subscribe<string>(
                exchange,
                queue,
                routingKey,
                10,
                (scope, @event) => throw new Exception());

            autoResetEvent.WaitOne();

            _channelMock.Verify(x => x.QueueDeclare(
                It.Is((string y) => y.EndsWith("-retry")),
                true,
                false,
                false,
                It.IsAny<IDictionary<string, object>>()), Times.Once());
            _channelMock.Verify(x => x.BasicPublish(
                exchange.Name.Value,
                It.Is((string y) => y.StartsWith(queue.Name.Value) && y.EndsWith("-retry")),
                false,
                _basicPropertiesMock.Object,
                It.IsAny<byte[]>()), Times.Once());
        }

        [Fact]
        public void GivenConnectionWhenSubscribeShouldExecuteCallbackLogOnFailure()
        {
            var exchange = Exchange.Create("test", ExchangeType.Direct);
            var queue = Queue.Create("test.requested");
            var routingKey = RoutingKey.Create("test.route");
            var body = Encoding.UTF8.GetBytes("test");
            const ushort deliveryTag = 1;

            var loggerMock = new Mock<IBusLogger>();

            _busSerializerMock.Setup(x => x.Deserialize<string>(body))
                .ReturnsAsync("test")
                .Verifiable();
            _serviceProviderMock.Setup(x => x.GetService(typeof(IBusLogger)))
                .Returns(loggerMock.Object)
                .Verifiable();

            var autoResetEvent = new AutoResetEvent(false);

            _channelMock.Setup(x => x.BasicConsume(
                    queue.Name.Value,
                    false,
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<IDictionary<string, object>>(),
                    It.IsAny<IBasicConsumer>()))
                .Callback((string queueName, bool autoAck, string consumerTag, bool noLocal, bool exclusive,
                    IDictionary<string, object> _, IBasicConsumer consumer) =>
                {
                    ((AsyncEventingBasicConsumer) consumer).HandleBasicDeliver(
                        consumerTag,
                        deliveryTag,
                        false,
                        exchange.Name.Value,
                        routingKey.Value,
                        _basicPropertiesMock.Object,
                        body).Wait();
                })
                .Returns(Guid.NewGuid().ToString());

            _channelMock.Setup(x => x.BasicNack(deliveryTag, false, false))
                .Callback((ulong tag, bool multiple, bool requeue) => autoResetEvent.Set())
                .Verifiable();

            _busConnection.Subscribe<string>(
                exchange,
                queue,
                routingKey,
                10,
                (scope, @event) => throw new Exception());

            autoResetEvent.WaitOne();

            loggerMock.Verify(x => x.WriteException(It.IsAny<string>(), It.IsAny<Exception>(),
                It.IsAny<KeyValuePair<string, object>[]>()));
        }
    }
}