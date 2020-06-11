using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Seedwork.CQRS.Bus.Core.Configurations;
using Xunit;

namespace Seedwork.CQRS.Bus.Core.Tests.IntegrationTests
{
    public class BusConnectionTests : IClassFixture<BusConnectionFixture>
    {
        public BusConnectionTests(BusConnectionFixture connectionFixture)
        {
            _connectionFixture = connectionFixture;
        }

        private readonly BusConnectionFixture _connectionFixture;

        [Fact]
        public void GivenConnectionWhenDisposeTwiceShouldNotThrow()
        {
            _connectionFixture.Connection.Dispose();
            _connectionFixture.Connection
                .Invoking(x => x.Dispose())
                .Should().NotThrow();
        }

        [Fact]
        public void GivenConnectionWhenPublishBatchShouldQueueHaveBatchLength()
        {
            var exchange = Exchange.Create("seedwork-cqrs-bus.integration-tests", ExchangeType.Direct);
            var queue = Queue.Create($"seedwork-cqrs-bus.integration-tests.queue-{Guid.NewGuid()}")
                .WithAutoDelete();
            var routingKey = RoutingKey.Create(queue.Name.Value);
            const string notification = "Notification message";

            var autoResetEvent = new AutoResetEvent(false);
            _connectionFixture.Connection.PublishSuccessed += _ => autoResetEvent.Set();

            _connectionFixture.Connection.PublishBatch(exchange, queue, routingKey, new[]
            {
                notification,
                notification
            });

            autoResetEvent.WaitOne();

            _connectionFixture.Connection.MessageCount(queue).Should().Be(2);
        }

        [Fact]
        public void GivenConnectionWhenPublishMessageShouldQueueHaveCountOne()
        {
            var exchange = Exchange.Create("seedwork-cqrs-bus.integration-tests", ExchangeType.Direct);
            var queue = Queue.Create($"seedwork-cqrs-bus.integration-tests.queue-{Guid.NewGuid()}")
                .WithAutoDelete();
            var routingKey = RoutingKey.Create(queue.Name.Value);
            const string notification = "Notification message";

            var autoResetEvent = new AutoResetEvent(false);
            _connectionFixture.Connection.PublishSuccessed += _ => autoResetEvent.Set();

            _connectionFixture.Connection.Publish(exchange, queue, routingKey, notification);

            autoResetEvent.WaitOne();

            _connectionFixture.Connection.MessageCount(queue).Should().Be(1);
        }

        [Fact]
        public void GivenConnectionWhenPublishShouldAddRetryProperties()
        {
            var exchange = Exchange.Create("seedwork-cqrs-bus.integration-tests", ExchangeType.Direct);
            var queue = Queue.Create($"seedwork-cqrs-bus.integration-tests.queue-{Guid.NewGuid()}")
                .WithAutoDelete();
            var routingKey = RoutingKey.Create(queue.Name.Value);
            object data = "Notification message";
            var message = Message.Create(data, 10);

            var autoResetEvent = new AutoResetEvent(false);
            _connectionFixture.Connection.PublishSuccessed += _ => autoResetEvent.Set();

            _connectionFixture.Connection.Publish(exchange, queue, routingKey, message);

            autoResetEvent.WaitOne();

            var responseMessage = _connectionFixture.Connection.GetMessage(queue);
            responseMessage.Should().NotBeNull();
            responseMessage.Data.Should().Be(data);
            responseMessage.AttemptCount.Should().Be(1);
            responseMessage.MaxAttempts.Should().Be(10);
        }

        [Fact]
        public void GivenConnectionWhenSubscribeAndThrowAndMaxAttemptsAchievedShouldQueueOnFailedQueue()
        {
            var exchange = Exchange.Create("seedwork-cqrs-bus.integration-tests", ExchangeType.Direct);
            var queue = Queue.Create($"seedwork-cqrs-bus.integration-tests.queue-{Guid.NewGuid()}");
            var routingKey = RoutingKey.Create(queue.Name.Value);
            var message = new TestMessage<string>(null, 1, 1, null, null);

            var autoResetEvent = new AutoResetEvent(false);

            _connectionFixture.Connection.PublishSuccessed += items =>
            {
                if (items.Any(x => x.Queue.Name.Value.EndsWith("-failed")))
                {
                    autoResetEvent.Set();
                }
            };

            _connectionFixture.Connection.Publish(exchange, queue, routingKey, message);

            _connectionFixture.Connection.Subscribe<string>(exchange, queue, routingKey, 1, (scope, m) =>
            {
                autoResetEvent.Set();
                throw new Exception();
            });

            autoResetEvent.WaitOne(); // Wait for subscribe to execute.

            autoResetEvent.WaitOne(); // Wait for failed message publishing.

            var failedQueue = Queue.Create($"{queue.Name.Value}-failed");

            _connectionFixture.Connection.MessageCount(failedQueue).Should().Be(1);
        }

        [Fact]
        public void GivenConnectionWhenSubscribeAndThrowShouldRequeueOnRetryQueue()
        {
            var exchange = Exchange.Create("seedwork-cqrs-bus.integration-tests", ExchangeType.Direct);
            var queue = Queue.Create($"seedwork-cqrs-bus.integration-tests.queue-{Guid.NewGuid()}");
            var routingKey = RoutingKey.Create(queue.Name.Value);
            const string notification = "Notification message";

            var autoResetEvent = new AutoResetEvent(false);

            _connectionFixture.Connection.PublishSuccessed += items =>
            {
                if (items.Any(x => x.Queue.Name.Value.EndsWith("-retry-1m")))
                {
                    autoResetEvent.Set();
                }
            };

            _connectionFixture.Connection.Publish(exchange, queue, routingKey, notification);

            _connectionFixture.Connection.Subscribe<string>(exchange, queue, routingKey, 1, (scope, message) =>
            {
                autoResetEvent.Set();
                throw new Exception();
            });

            autoResetEvent.WaitOne(); // Wait for subscribe to execute.

            autoResetEvent.WaitOne(); // Wait for retry message publishing.

            var retryQueue = Queue.Create($"{queue.Name.Value}-retry-1m");

            _connectionFixture.Connection.MessageCount(retryQueue).Should().Be(1);
        }
        
        [Fact]
        public void GivenConnectionWhenSubscribeAndThrowShouldRequeueOnRetryQueueTheRightMessage()
        {
            var exchange = Exchange.Create("seedwork-cqrs-bus.integration-tests", ExchangeType.Direct);
            var queue = Queue.Create($"seedwork-cqrs-bus.integration-tests.queue-{Guid.NewGuid()}");
            var routingKey = RoutingKey.Create(queue.Name.Value);
            const string notification = "Notification message";

            var autoResetEvent = new AutoResetEvent(false);

            _connectionFixture.Connection.PublishSuccessed += items =>
            {
                if (items.Any(x => x.Queue.Name.Value.EndsWith("-retry-1m")))
                {
                    autoResetEvent.Set();
                }
            };

            _connectionFixture.Connection.Publish(exchange, queue, routingKey, notification);

            _connectionFixture.Connection.Subscribe<string>(exchange, queue, routingKey, 1, (scope, message) =>
            {
                autoResetEvent.Set();
                throw new Exception();
            });

            autoResetEvent.WaitOne(); // Wait for subscribe to execute.

            autoResetEvent.WaitOne(); // Wait for retry message publishing.

            var retryQueue = Queue.Create($"{queue.Name.Value}-retry-1m");

            _connectionFixture.Connection.MessageCount(retryQueue).Should().Be(1);
            var message = _connectionFixture.Connection.GetMessage(retryQueue);
            message.Should().NotBeNull();
            message.Data.Should().Be(notification);
        }
        
        [Fact]
        public void GivenConnectionWhenSubscribeAndFailShouldRequeueOnRetryQueueTheRightMessage()
        {
            var exchange = Exchange.Create("seedwork-cqrs-bus.integration-tests", ExchangeType.Direct);
            var queue = Queue.Create($"seedwork-cqrs-bus.integration-tests.queue-{Guid.NewGuid()}");
            var routingKey = RoutingKey.Create(queue.Name.Value);
            const string notification = "Notification message";

            var autoResetEvent = new AutoResetEvent(false);

            _connectionFixture.Connection.PublishSuccessed += items =>
            {
                if (items.Any(x => x.Queue.Name.Value.EndsWith("-retry-1m")))
                {
                    autoResetEvent.Set();
                }
            };

            _connectionFixture.Connection.Publish(exchange, queue, routingKey, notification);

            Message<string> consumerMessage = null;

            _connectionFixture.Connection.Subscribe<string>(exchange, queue, routingKey, 1, (scope, m) =>
            {
                autoResetEvent.Set();
                consumerMessage = m;
                return Task.CompletedTask;
            }, false);

            autoResetEvent.WaitOne(); // Wait for subscribe to execute.
            
            consumerMessage.Fail(new Exception());

            autoResetEvent.WaitOne(); // Wait for retry message publishing.

            var retryQueue = Queue.Create($"{queue.Name.Value}-retry-1m");

            _connectionFixture.Connection.MessageCount(retryQueue).Should().Be(1);
            var message = _connectionFixture.Connection.GetMessage(retryQueue);
            message.Should().NotBeNull();
            message.Data.Should().Be(notification);
        }

        [Fact]
        public void GivenConnectionWhenSubscribeShouldExecuteCallback()
        {
            var exchange = Exchange.Create("seedwork-cqrs-bus.integration-tests", ExchangeType.Direct);
            var queue = Queue.Create($"seedwork-cqrs-bus.integration-tests.queue-{Guid.NewGuid()}")
                .WithAutoDelete();
            var routingKey = RoutingKey.Create(queue.Name.Value);
            const string notification = "Notification message";

            _connectionFixture.Connection.Publish(exchange, queue, routingKey, notification);

            IServiceScope callbackScope = null;
            string callbackMessage = null;

            var autoResetEvent = new AutoResetEvent(false);

            _connectionFixture.Connection.Subscribe<string>(exchange, queue, routingKey, 1, (scope, message) =>
            {
                callbackScope = scope;
                callbackMessage = message;
                autoResetEvent.Set();

                return Task.CompletedTask;
            });

            autoResetEvent.WaitOne();

            callbackScope.Should().NotBeNull();
            callbackMessage.Should().Be(notification);
        }

        [Fact]
        public void GivenConnectionWhenRetryShouldUseConfiguredBehavior()
        {
            var configuration = new ConfigurationBuilder()
                .Build();
            
            var retryBehaviorMock = new Mock<IRetryBehavior>();
            var serviceProvider = new ServiceCollection()
                .AddBusCore(configuration, builder =>
                {
                    builder.SetConnectionString("amqp://guest:guest@localhost:5672/")
                        .IgnoreCertificate()
                        .SetSerializer<BusSerializer>()
                        .UseRetryBehabior(retryBehaviorMock.Object);
                })
                .BuildServiceProvider();

            var connection = serviceProvider.GetService<BusConnection>();

            var exchange = Exchange.Default;
            var queue = Queue.Create(Guid.NewGuid().ToString());
            var routingKey = RoutingKey.Create(queue.Name.Value);
            BatchItem item = null;
            
            connection.Publish(exchange, queue, routingKey, "Message");

            var autoResetEvent = new AutoResetEvent(false);
            connection.PublishSuccessed += items =>
            {
                item = items.First();
                autoResetEvent.Set();
            };

            autoResetEvent.WaitOne();

            retryBehaviorMock.Setup(x => x.ShouldRetry(1, 5))
                .Returns(true)
                .Verifiable();
            retryBehaviorMock.Setup(x => x.GetWaitTime(1))
                .Returns(TimeSpan.FromMinutes(5))
                .Verifiable();
            
            connection.Subscribe<string>(exchange, queue, routingKey, 1, (scope, message) =>
            {
                throw new Exception("Test");
            });

            autoResetEvent.WaitOne();

            item.Should().NotBeNull();
            item.Queue.Name.Value.Should().EndWith("5m");
            retryBehaviorMock.VerifyAll();
        }
    }
}