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
            var channel = Connection.CreateModel();
            channel.BasicQos(0, prefetchCount, false);
            exchange.Declare(channel);
            queue.Declare(channel);
            queue.Bind(channel, exchange.Name, routingKey);

            var consumer = new AsyncEventingBasicConsumer(channel);
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
                        var autorecoveringChannel = (AutorecoveringModel) channel;
                        autorecoveringChannel.AutomaticallyRecover((AutorecoveringConnection) Connection, null);
                    });
            };
            consumer.Received += (sender, args) =>
            {
                Task.Run(async () =>
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var dto = await _serializer.Deserialize<T>(args.Body);
                        try
                        {
                            await action.Invoke(scope, dto);
                            channel.BasicAck(args.DeliveryTag, false);
                        }
                        catch (Exception exception)
                        {
                            channel.BasicNack(args.DeliveryTag, false, true);

                            var logger = scope.ServiceProvider.GetService<IBusLogger>();
                            if (logger == null)
                            {
                                return;
                            }

                            await logger.WriteException(typeof(T).Name, exception,
                                new KeyValuePair<string, object>("Event", dto));
                        }
                    }
                });
                return Task.CompletedTask;
            };

            var consumerTag = channel.BasicConsume(queue.Name.Value, false, consumer);
            _consumers.GetOrAdd(consumerTag, (channel, consumer));
        }

        public async Task Publish(Exchange exchange, Queue queue, RoutingKey routingKey, object notification)
        {
            var body = await _serializer.Serialize(notification);
            using (var channel = Connection.CreateModel())
            {
                exchange.Declare(channel);
                queue.Declare(channel);
                queue.Bind(channel, exchange.Name, routingKey);
                channel.BasicPublish(exchange.Name.Value, routingKey.Value, false, null, body);
                channel.Close();
            }
        }

        public async Task PublishBatch(Exchange exchange, Queue queue, RoutingKey routingKey,
            IEnumerable<object> notification)
        {
            using (var channel = Connection.CreateModel())
            {
                exchange.Declare(channel);
                queue.Declare(channel);
                queue.Bind(channel, exchange.Name, routingKey);
                var bodiesTask = notification.Select(obj => _serializer.Serialize(obj)).ToList();

                await Task.WhenAll(bodiesTask);

                var batch = channel.CreateBasicPublishBatch();
                foreach (var task in bodiesTask)
                {
                    var body = task.Result;
                    batch.Add(exchange.Name.Value, routingKey.Value, false, null, body);
                }

                batch.Publish();
                channel.Close();
            }
        }

        public async Task Publish(Exchange exchange, RoutingKey routingKey, object notification)
        {
            var body = await _serializer.Serialize(notification);
            using (var channel = Connection.CreateModel())
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
                NetworkRecoveryInterval = TimeSpan.FromSeconds(30),
                DispatchConsumersAsync = true
            };
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