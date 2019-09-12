using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BufferList;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Seedwork.CQRS.Bus.Core
{
    public class BusConnection : IDisposable
    {
        public const int PublisherBufferSize = 1000;
        public const int PublisherBufferTtlInSeconds = 5;
        public const int ConnectionMaxRetry = 10;
        public const int ConnectionRetryDelayInMs = 500;
        public const int ConsumerMaxParallelTasks = 500;
        public const int MessageMaxRetry = 5;
        public const int PublishMaxRetry = 5;
        public const int PublishRetryDelayInMs = 100;
        private static volatile object _sync = new object();
        private readonly IConnectionFactory _connectionFactory;
        private readonly ConcurrentDictionary<string, (IModel, AsyncEventingBasicConsumer, List<Task>)> _consumers;
        private readonly IBusLogger _logger;
        private readonly BufferList<BatchItem> _publisherBuffer;
        private readonly IBusSerializer _serializer;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private IConnection _consumerConnection;
        private bool _disposed;
        private IConnection _publisherConnection;

        public BusConnection(IConnectionFactory connectionFactory,
            IBusSerializer serializer,
            IServiceScopeFactory serviceScopeFactory,
            IBusLogger logger)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _consumers = new ConcurrentDictionary<string, (IModel, AsyncEventingBasicConsumer, List<Task>)>();
            _connectionFactory = connectionFactory;
            _publisherBuffer =
                new BufferList<BatchItem>(PublisherBufferSize, TimeSpan.FromSeconds(PublisherBufferTtlInSeconds));
            _publisherBuffer.Cleared += PublisherBufferOnCleared;
        }

        public BusConnection(BusConnectionString connectionString,
            IBusSerializer serializer,
            IServiceScopeFactory serviceScopeFactory,
            IBusLogger logger) : this(GetConnectionFactory(connectionString.Value), serializer,
            serviceScopeFactory, logger)
        {
        }

        public IConnection PublisherConnection
        {
            get
            {
                if (_publisherConnection != null)
                {
                    return _publisherConnection;
                }

                lock (_sync)
                {
                    return _publisherConnection ?? (_publisherConnection = Policy.Handle<Exception>()
                               .WaitAndRetry(ConnectionMaxRetry,
                                   _ => TimeSpan.FromMilliseconds(ConnectionRetryDelayInMs))
                               .Execute(() => _connectionFactory.CreateConnection()));
                }
            }
        }

        public IConnection ConsumerConnection
        {
            get
            {
                if (_consumerConnection != null)
                {
                    return _consumerConnection;
                }

                lock (_sync)
                {
                    return _consumerConnection ?? (_consumerConnection = Policy.Handle<Exception>()
                               .WaitAndRetry(ConnectionMaxRetry,
                                   _ => TimeSpan.FromMilliseconds(ConnectionRetryDelayInMs))
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
            var channel = ConsumerConnection.CreateModel();
            channel.BasicQos(0, prefetchCount, false);
            exchange.Declare(channel);
            queue.Declare(channel);
            queue.Bind(channel, exchange, routingKey);

            var consumer = new AsyncEventingBasicConsumer(channel);

            var tasks = new List<Task>(ConsumerMaxParallelTasks);
            consumer.Received += async (sender, args) =>
            {
                try
                {
                    var task = Task.Run(async () =>
                    {
                        var message = Message.Create<T>(_serializer, args);
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            try
                            {
                                await action.Invoke(scope, (T) message.Data);
                                channel.BasicAck(args.DeliveryTag, false);
                            }
                            catch (Exception exception)
                            {
                                if (message.CanRetry())
                                {
                                    var retryQueue = queue.CreateRetryQueue(TimeSpan.FromMinutes(1), exchange,
                                        routingKey);
                                    await Publish(exchange, retryQueue, RoutingKey.Create(retryQueue.Name.Value),
                                        message);
                                }
                                else
                                {
                                    var failedQueue = queue.CreateFailedQueue();
                                    await Publish(exchange, failedQueue, RoutingKey.Create(failedQueue.Name.Value),
                                        message);
                                }

                                channel.BasicAck(args.DeliveryTag, false);

                                await _logger.WriteException(typeof(T).Name, exception,
                                    new KeyValuePair<string, object>("Event", message));
                            }
                        }
                    });
                    tasks.Add(task);
                }
                catch (Exception ex)
                {
                    channel.BasicNack(args.DeliveryTag, false, true);
                    await _logger.WriteException(typeof(T).Name, ex,
                        new KeyValuePair<string, object>("args", args));
                }
            };

            var consumerTag = channel.BasicConsume(queue.Name.Value, false, consumer);
            _consumers.GetOrAdd(consumerTag, (channel, consumer, tasks));
        }

        public Task Publish(Exchange exchange, Queue queue, RoutingKey routingKey, Message message)
        {
            _publisherBuffer.Add(new BatchItem(exchange, queue, routingKey, message));
            return Task.CompletedTask;
        }

        public async Task Publish(Exchange exchange, Queue queue, RoutingKey routingKey, object data)
        {
            await Publish(exchange, queue, routingKey, Message.Create(data, MessageMaxRetry));
        }

        public async Task PublishBatch(Exchange exchange, Queue queue, RoutingKey routingKey,
            IEnumerable<object> notification)
        {
            var messages = notification.Select(x => Message.Create(x, MessageMaxRetry));
            await PublishBatch(exchange, queue, routingKey, messages);
        }

        public Task PublishBatch(Exchange exchange, Queue queue, RoutingKey routingKey,
            IEnumerable<Message> messages)
        {
            var batches = messages.Select(x => new BatchItem(exchange, queue, routingKey, x));
            foreach (var item in batches)
            {
                _publisherBuffer.Add(item);
            }

            return Task.CompletedTask;
        }

        public Task Publish(Exchange exchange, RoutingKey routingKey, object notification)
        {
            _publisherBuffer.Add(new BatchItem(exchange, null, routingKey,
                Message.Create(notification, MessageMaxRetry)));
            return Task.CompletedTask;
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
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                DispatchConsumersAsync = true
            };
        }

        private void PublisherBufferOnCleared(IEnumerable<BatchItem> removedItems)
        {
            Policy
                .Handle<Exception>()
                .WaitAndRetry(PublishMaxRetry, _ => TimeSpan.FromMilliseconds(PublishRetryDelayInMs),
                    (exception, span) =>
                    {
                        _logger.WriteException("Publisher", exception,
                            new KeyValuePair<string, object>("Events", removedItems)).GetAwaiter().GetResult();
                    })
                .Execute(() =>
                {
                    using (var channel = PublisherConnection.CreateModel())
                    {
                        var batch = channel.CreateBasicPublishBatch();
                        try
                        {
                            foreach (var group in removedItems.GroupBy(x => (x.Exchange, x.Queue, x.RoutingKey)))
                            {
                                var (exchange, queue, routingKey) = group.Key;
                                exchange.Declare(channel);
                                queue?.Declare(channel);
                                queue?.Bind(channel, exchange, routingKey);
                                foreach (var item in group)
                                {
                                    var (body, basicProperties) =
                                        item.Message.GetData(channel, _serializer);
                                    batch.Add(exchange.Name.Value, routingKey.Value, false,
                                        basicProperties, body);
                                }
                            }

                            batch.Publish();
                        }
                        catch (Exception ex)
                        {
                            _logger?.WriteException("Publish", ex,
                                new KeyValuePair<string, object>("Events", removedItems)).GetAwaiter().GetResult();
                        }
                        finally
                        {
                            channel.Close();
                        }
                    }
                });
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
                    var (channel, _, buffer) = value;
                    buffer.Clear();
                    channel.BasicCancel(key);
                    channel.Close();
                    channel.Dispose();
                }

                _consumerConnection.Close();
                _consumerConnection.Dispose();
            }

            _disposed = true;
        }
    }
}