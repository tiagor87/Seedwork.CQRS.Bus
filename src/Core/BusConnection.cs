using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TRBufferList.Core;

namespace Seedwork.CQRS.Bus.Core
{
    public delegate void PublishSuccessed(IEnumerable<BatchItem> items);

    public delegate void PublishFailed(IEnumerable<BatchItem> items, Exception exception);

    public class BusConnection : IDisposable
    {
        private static volatile object _sync = new object();
        private readonly IConnectionFactory _connectionFactory;
        private readonly ConcurrentDictionary<string, (IModel, AsyncEventingBasicConsumer, Tasks)> _consumers;
        private readonly IBusLogger _logger;
        private readonly IOptions<BusConnectionOptions> _options;
        private readonly BufferList<BatchItem> _publisherBuffer;
        private readonly IBusSerializer _serializer;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private IConnection _consumerConnection;
        private bool _disposed;
        private IConnection _publisherConnection;

        public BusConnection(IConnectionFactory connectionFactory,
            IBusSerializer serializer,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<BusConnectionOptions> options,
            IBusLogger logger = null)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _options = options;
            _consumers = new ConcurrentDictionary<string, (IModel, AsyncEventingBasicConsumer, Tasks)>();
            _connectionFactory = connectionFactory;
            _publisherBuffer =
                new BufferList<BatchItem>(_options.Value.PublisherBufferSize,
                    TimeSpan.FromMilliseconds(_options.Value.PublisherBufferTtlInMilliseconds));
            _publisherBuffer.Cleared += PublishBufferOnCleared;
        }

        public BusConnection(BusConnectionString connectionString,
            IBusSerializer serializer,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<BusConnectionOptions> options,
            IBusLogger logger = null) : this(GetConnectionFactory(connectionString.Value), serializer,
            serviceScopeFactory, options, logger)
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

                var retryDelay = TimeSpan.FromMilliseconds(_options.Value.ConnectionRetryDelayInMilliseconds);
                lock (_sync)
                {
                    return _publisherConnection ??= Policy.Handle<Exception>()
                        .WaitAndRetry(_options.Value.ConnectionMaxRetry,
                            _ => retryDelay)
                        .Execute(() => _connectionFactory.CreateConnection());
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

                var retryDelay = TimeSpan.FromMilliseconds(_options.Value.ConnectionRetryDelayInMilliseconds);
                lock (_sync)
                {
                    return _consumerConnection ??= Policy.Handle<Exception>()
                        .WaitAndRetry(
                            _options.Value.ConnectionMaxRetry,
                            _ => retryDelay)
                        .Execute(() => _connectionFactory.CreateConnection());
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public event PublishSuccessed PublishSuccessed;
        public event PublishFailed PublishFailed;

        public void Declare(Exchange exchange, Queue queue, params RoutingKey[] routingKeys)
        {
            using (var channel = PublisherConnection.CreateModel())
            {
                exchange.Declare(channel);
                queue.Declare(channel);
                foreach (var key in routingKeys)
                {
                    queue.Bind(channel, exchange, key);
                }

                channel.Close();
            }
        }

        public void Subscribe<T>(Exchange exchange, Queue queue, RoutingKey routingKey, ushort prefetchCount,
            Func<IServiceScope, Message<T>, Task> action, bool autoAck = true)
        {
            var channel = ConsumerConnection.CreateModel();
            channel.BasicQos(0, prefetchCount, false);
            Declare(exchange, queue, routingKey);

            var consumer = new AsyncEventingBasicConsumer(channel);

            var tasks = new Tasks(_options.Value.ConsumerMaxParallelTasks);
            consumer.Received += (sender, args) =>
            {
                tasks.WaitForFreeSlots();
                try
                {
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            var message = Message<T>.Create(channel, exchange, queue, routingKey, _serializer, args,
                                (OnDone, OnFail));
                            using (var scope = _serviceScopeFactory.CreateScope())
                            {
                                try
                                {
                                    await action.Invoke(scope, message);
                                    if (autoAck) message.Complete();
                                }
                                catch (Exception exception)
                                {
                                    message.Fail(exception);
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            _logger?.WriteException(nameof(Subscribe), exception,
                                new KeyValuePair<string, object>("Event", Encoding.UTF8.GetString(args.Body.ToArray())));
                            var failedQueue = queue.CreateFailedQueue();
                            var failedRoutingKey = RoutingKey.Create(failedQueue.Name.Value);
                            Publish(Exchange.Default, failedQueue, failedRoutingKey,
                                ErrorMessage.Create(args.Body.ToArray(), args.BasicProperties));
                            channel.BasicNack(args.DeliveryTag, false, false);
                        }
                    });
                    tasks.Add(task);
                }
                catch (Exception ex)
                {
                    _logger?.WriteException(typeof(T).Name, ex,
                        new KeyValuePair<string, object>("args", args));
                    channel.BasicNack(args.DeliveryTag, false, true);
                }

                return Task.CompletedTask;
            };

            var consumerTag = channel.BasicConsume(queue.Name.Value, false, consumer);
            _consumers.GetOrAdd(consumerTag, (channel, consumer, tasks));
        }

        public void Publish(Exchange exchange, Queue queue, RoutingKey routingKey, Message message)
        {
            _publisherBuffer.Add(new BatchItem(exchange, queue, routingKey, message));
        }

        public void Publish(Exchange exchange, Queue queue, RoutingKey routingKey, object data)
        {
            Publish(exchange, queue, routingKey, Message.Create(data, _options.Value.MessageMaxRetry));
        }

        public void Publish(Exchange exchange, RoutingKey routingKey, object notification)
        {
            _publisherBuffer.Add(new BatchItem(exchange, null, routingKey,
                Message.Create(notification, _options.Value.MessageMaxRetry)));
        }

        public void PublishBatch(Exchange exchange, Queue queue, RoutingKey routingKey,
            IEnumerable<object> notification)
        {
            var messages = notification.Select(x => Message.Create(x, _options.Value.MessageMaxRetry));
            PublishBatch(exchange, queue, routingKey, messages);
        }

        public void PublishBatch(Exchange exchange, Queue queue, RoutingKey routingKey,
            IEnumerable<Message> messages)
        {
            var batches = messages.Select(x => new BatchItem(exchange, queue, routingKey, x));
            foreach (var item in batches)
            {
                _publisherBuffer.Add(item);
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
                RequestedHeartbeat = TimeSpan.FromSeconds(30),
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                DispatchConsumersAsync = true
            };
        }

        private void OnDone<T>(Message<T> message) => message.Channel.BasicAck(message.DeliveryTag, false);

        private void OnFail<T>(Exception exception, Message<T> message)
        {
            if (message.CanRetry())
            {
                var retryQueue = message.Queue
                    .CreateRetryQueue(TimeSpan.FromMinutes(1));
                var retryRoutingKey = RoutingKey.Create(retryQueue.Name.Value);
                Publish(Exchange.Default, retryQueue, retryRoutingKey, message);
            }
            else
            {
                var failedQueue = message.Queue
                    .CreateFailedQueue();
                var failedRoutingKey = RoutingKey.Create(failedQueue.Name.Value);
                Publish(Exchange.Default, failedQueue, failedRoutingKey, message);
            }

            message.Channel.BasicAck(message.DeliveryTag, false);

            _logger?.WriteException(typeof(T).Name, exception, new KeyValuePair<string, object>("Event", message));
        }

        private void PublishBufferOnCleared(IEnumerable<BatchItem> removedItems)
        {
            var items = removedItems.ToList();
            try
            {
                Policy
                    .Handle<Exception>()
                    .WaitAndRetry(
                        _options.Value.PublishMaxRetry,
                        _ => TimeSpan.FromMilliseconds(_options.Value.PublishRetryDelayInMilliseconds),
                        (exception, span) =>
                        {
                            _logger?.WriteException("Publisher", exception,
                                new KeyValuePair<string, object>("Events", removedItems));
                        })
                    .Execute(() =>
                    {
                        using (var channel = PublisherConnection.CreateModel())
                        {
                            var batch = channel.CreateBasicPublishBatch();
                            try
                            {
                                foreach (var group in items.GroupBy(x => (x.Exchange, x.Queue, x.RoutingKey)))
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
                            finally
                            {
                                channel.Close();
                            }
                        }
                    });

                PublishSuccessed?.Invoke(items);
            }
            catch (Exception ex)
            {
                _logger?.WriteException(nameof(PublishBufferOnCleared), ex,
                    new KeyValuePair<string, object>("Events", items));
                if (PublishFailed == null)
                {
                    throw;
                }
                PublishFailed.Invoke(items, ex);
            }
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
                    var (channel, _, tasks) = value;
                    tasks.Dispose();
                    channel.BasicCancel(key);
                    channel.Close();
                    channel.Dispose();
                }

                _publisherBuffer.Clear();
                _consumerConnection?.Close();
                _consumerConnection?.Dispose();
                _publisherConnection?.Close();
                _publisherConnection?.Dispose();
            }

            _disposed = true;
        }
    }
}