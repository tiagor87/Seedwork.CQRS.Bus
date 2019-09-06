using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing.Impl;
using RabbitMQ.Client.Impl;

namespace Seedwork.CQRS.Bus.Core
{
    public class BusConnection : IDisposable
    {
        private static volatile object _sync = new object();
        private readonly IConnectionFactory _connectionFactory;
        private readonly ConcurrentDictionary<string, (IModel, AsyncEventingBasicConsumer)> _consumers;
        private readonly IBusSerializer _serializer;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private IConnection _connection;
        private bool _disposed;

        public BusConnection(IConnectionFactory connectionFactory,
            IBusSerializer serializer,
            IServiceScopeFactory serviceScopeFactory)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _serviceScopeFactory = serviceScopeFactory;
            _consumers = new ConcurrentDictionary<string, (IModel, AsyncEventingBasicConsumer)>();
            _connectionFactory = connectionFactory;
        }

        public BusConnection(BusConnectionString connectionString,
            IBusSerializer serializer,
            IServiceScopeFactory serviceScopeFactory) : this(GetConnectionFactory(connectionString.Value), serializer,
            serviceScopeFactory)
        {
        }

        public IConnection Connection
        {
            get
            {
                if (_connection != null)
                {
                    return _connection;
                }

                lock (_sync)
                {
                    return _connection ?? (_connection = Policy.Handle<BrokerUnreachableException>()
                               .RetryForever()
                               .Execute(() => _connectionFactory.CreateConnection()));
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Subscribe<T>(Exchange exchange, Queue queue, RoutingKey routingKey, ushort prefetchCount,
            Func<IServiceScope, T, Task> action)
        {
            var channel = CreateChannel();
            channel.BasicQos(0, prefetchCount, false);
            exchange.Declare(channel);
            queue.Declare(channel);
            queue.Bind(channel, exchange, routingKey);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.Received += (sender, args) =>
            {
                Task.Run(async () =>
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var message = Message.Create<T>(_serializer, args);
                        try
                        {
                            await action.Invoke(scope, (T) message.Data);
                            channel.BasicAck(args.DeliveryTag, false);
                        }
                        catch (Exception exception)
                        {
                            if (message.CanRetry())
                            {
                                var retryQueue = queue.CreateRetryQueue(TimeSpan.FromMinutes(1), exchange, routingKey);
                                await Publish(exchange, retryQueue, RoutingKey.Create(retryQueue.Name.Value), message);
                            }
                            else
                            {
                                var failedQueue = queue.CreateFailedQueue();
                                await Publish(exchange, failedQueue, RoutingKey.Create(failedQueue.Name.Value),
                                    message);
                            }

                            channel.BasicNack(args.DeliveryTag, false, false);

                            var logger = scope.ServiceProvider.GetService<IBusLogger>();
                            if (logger == null)
                            {
                                return;
                            }

                            await logger.WriteException(typeof(T).Name, exception,
                                new KeyValuePair<string, object>("Event", message));
                        }
                    }
                });
                return Task.CompletedTask;
            };

            var consumerTag = channel.BasicConsume(queue.Name.Value, false, consumer);
            _consumers.GetOrAdd(consumerTag, (channel, consumer));
        }

        public async Task Publish(Exchange exchange, Queue queue, RoutingKey routingKey, Message message)
        {
            await Policy.Handle<TimeoutException>()
                .RetryForeverAsync()
                .ExecuteAsync(async () =>
                {
                    using (var channel = CreateChannel())
                    {
                        exchange.Declare(channel);
                        queue.Declare(channel);
                        queue.Bind(channel, exchange, routingKey);
                        var (body, basicProperties) = await message.GetData(channel, _serializer);
                        channel.BasicPublish(exchange.Name.Value, routingKey.Value, false, basicProperties, body);
                        channel.Close();
                    }
                });
        }

        public async Task Publish(Exchange exchange, Queue queue, RoutingKey routingKey, object data)
        {
            await Publish(exchange, queue, routingKey, Message.Create(data, 5));
        }

        public async Task PublishBatch(Exchange exchange, Queue queue, RoutingKey routingKey,
            IEnumerable<object> notification)
        {
            var messages = notification.Select(x => Message.Create(x, 5));
            await PublishBatch(exchange, queue, routingKey, messages);
        }

        public async Task PublishBatch(Exchange exchange, Queue queue, RoutingKey routingKey,
            IEnumerable<Message> messages)
        {
            using (var channel = CreateChannel())
            {
                exchange.Declare(channel);
                queue.Declare(channel);
                queue.Bind(channel, exchange, routingKey);
                var tasks = messages.Select(m => m.GetData(channel, _serializer)).ToList();

                await Task.WhenAll(tasks);

                var batch = channel.CreateBasicPublishBatch();
                foreach (var task in tasks)
                {
                    var (body, properties) = task.Result;
                    batch.Add(exchange.Name.Value, routingKey.Value, false, properties, body);
                }

                batch.Publish();
                channel.Close();
            }
        }

        public async Task Publish(Exchange exchange, RoutingKey routingKey, object notification)
        {
            var body = await _serializer.Serialize(notification);
            using (var channel = CreateChannel())
            {
                exchange.Declare(channel);
                channel.BasicPublish(exchange.Name.Value, routingKey.Value, false, null, body);
                channel.Close();
            }
        }

        ~BusConnection()
        {
            Dispose(false);
        }

        private static IConnectionFactory GetConnectionFactory(Uri connectionString)
        {
            return new ConnectionFactory
            {
                Uri = connectionString,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                RequestedHeartbeat = 30,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                DispatchConsumersAsync = true
            };
        }

        private IModel CreateChannel()
        {
            var channel = Connection.CreateModel();
            channel.ModelShutdown += (sender, args) =>
            {
                if (args.Initiator == ShutdownInitiator.Application)
                {
                    return;
                }

                Policy.Handle<BrokerUnreachableException>()
                    .RetryForever()
                    .Execute(() =>
                    {
                        var autoRecoveringChannel = (AutorecoveringModel) channel;
                        autoRecoveringChannel.AutomaticallyRecover((AutorecoveringConnection) Connection, null);
                    });
            };
            return channel;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                foreach (var consumerGroup in _consumers)
                {
                    var key = consumerGroup.Key;
                    var value = consumerGroup.Value;
                    var (channel, _) = value;
                    channel.BasicCancel(key);
                    channel.Close();
                    channel.Dispose();
                }

                _connection.Close();
                _connection.Dispose();
            }

            _disposed = true;
        }
    }
}