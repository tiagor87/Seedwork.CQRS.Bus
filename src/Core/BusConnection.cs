using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private readonly uint _maxTasks = 50;
        private readonly IBusSerializer _serializer;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly List<Task> _tasks;
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
            _tasks = new List<Task>();
        }

        public BusConnection(BusConnectionString connectionString,
            IBusSerializer serializer,
            IServiceScopeFactory serviceScopeFactory) : this(GetConnectionFactory(connectionString.Value), serializer,
            serviceScopeFactory)
        {
            _maxTasks = connectionString.MaxThreads;
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
                while (_tasks.Count == _maxTasks)
                {
                    _tasks.RemoveAll(x => x.IsCompleted);
                    Thread.Sleep(100);
                }

                var task = Task.Run(async () =>
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
                _tasks.Add(task);
                return Task.CompletedTask;
            };

            var consumerTag = channel.BasicConsume(queue.Name.Value, false, consumer);
            _consumers.GetOrAdd(consumerTag, (channel, consumer));
        }

        public Task Publish(Exchange exchange, Queue queue, RoutingKey routingKey, Message message)
        {
            var task = Policy.Handle<TimeoutException>()
                .RetryForeverAsync()
                .ExecuteAsync(() =>
                {
                    using (var channel = CreateChannel())
                    {
                        exchange.Declare(channel);
                        queue.Declare(channel);
                        queue.Bind(channel, exchange, routingKey);
                        var (body, basicProperties) = message.GetData(channel, _serializer);
                        channel.BasicPublish(exchange.Name.Value, routingKey.Value, false, basicProperties, body);
                        channel.Close();
                    }

                    return Task.CompletedTask;
                });
            _tasks.Add(task);
            return Task.CompletedTask;
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

        public Task PublishBatch(Exchange exchange, Queue queue, RoutingKey routingKey,
            IEnumerable<Message> messages)
        {
            var task = Policy.Handle<TimeoutException>()
                .RetryForeverAsync()
                .ExecuteAsync(() =>
                {
                    using (var channel = CreateChannel())
                    {
                        exchange.Declare(channel);
                        queue.Declare(channel);
                        queue.Bind(channel, exchange, routingKey);
                        var batch = channel.CreateBasicPublishBatch();
                        foreach (var message in messages.ToList())
                        {
                            var (body, properties) = message.GetData(channel, _serializer);
                            batch.Add(exchange.Name.Value, routingKey.Value, false, properties, body);
                        }

                        batch.Publish();
                        channel.Close();
                    }

                    return Task.CompletedTask;
                });
            _tasks.Add(task);
            return Task.CompletedTask;
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
                Task.WaitAll(_tasks.ToArray());
                _tasks.Clear();
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