using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using Seedwork.CQRS.Bus.Core.Consumers;
using Seedwork.CQRS.Bus.Core.Consumers.Abstractions;
using Seedwork.CQRS.Bus.Core.Consumers.Options;
using Seedwork.CQRS.Bus.Core.RetryBehaviors;
using TRBufferList.Core;

namespace Seedwork.CQRS.Bus.Core
{
    public delegate void PublishSuccessed(IEnumerable<BatchItem> items);

    public delegate void PublishFailed(IEnumerable<BatchItem> items, Exception exception);

    public class BusConnection : IBusConnection
    {
        private static volatile object _sync = new object();
        private readonly IConnectionFactory _connectionFactory;
        private readonly ConcurrentBag<IConsumer> _consumers;
        private readonly IBusLogger _logger;
        private readonly IOptions<BusConnectionOptions> _options;
        private readonly BufferList<BatchItem> _publisherBuffer;
        private readonly IBusSerializer _serializer;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private IConnection _consumerConnection;
        private bool _disposed;
        private IConnection _publisherConnection;
        private readonly IRetryBehavior _retryBehavior;

        public BusConnection(IConnectionFactory connectionFactory,
            IBusSerializer serializer,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<BusConnectionOptions> options,
            IBusLogger logger = null,
            IRetryBehavior retryBehavior = null)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _options = options;
            _consumers = new ConcurrentBag<IConsumer>();
            _connectionFactory = connectionFactory;
            _publisherBuffer =
                new BufferList<BatchItem>(_options.Value.PublisherBufferSize,
                    TimeSpan.FromMilliseconds(_options.Value.PublisherBufferTtlInMilliseconds));
            _publisherBuffer.Cleared += PublishBufferOnCleared;
            _retryBehavior = retryBehavior ?? new ConstantRetryBehavior(1);
        }

        public BusConnection(BusConnectionString connectionString,
            IBusSerializer serializer,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<BusConnectionOptions> options,
            IBusLogger logger = null,
            IRetryBehavior retryBehavior = null) : this(GetConnectionFactory(connectionString), serializer,
            serviceScopeFactory, options, logger, retryBehavior)
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
            var consumer = new Consumer<T>(
                this,
                _logger,
                _retryBehavior,
                _serviceScopeFactory,
                new ConsumerOptions<T>(_serializer, exchange, queue, prefetchCount, action, autoAck, _options.Value.ConsumerMaxParallelTasks, routingKey));

            _consumers.Add(consumer);
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

        public void Flush()
        {
            _publisherBuffer.Clear();
            Task.Delay(1000).Wait();
        } 

        ~BusConnection()
        {
            Dispose(false);
        }

        private static IConnectionFactory GetConnectionFactory(BusConnectionString connectionString)
        {
            var factory = new ConnectionFactory
            {
                Uri = connectionString.Value,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                RequestedHeartbeat = 30,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                DispatchConsumersAsync = true
            };
            
            if (!connectionString.ValidateCertificate)
            {
                //NOSONAR
                factory.Ssl.CertificateValidationCallback = (sender, certificate, chain, errors) => true;
                //NOSONAR
                factory.Ssl.AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors |
                                                     SslPolicyErrors.RemoteCertificateNotAvailable |
                                                     SslPolicyErrors.RemoteCertificateNameMismatch;
            }

            return factory;
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
                        using var channel = PublisherConnection.CreateModel();
                        channel.ConfirmSelect();
                        
                        var batch = channel.CreateBasicPublishBatch();
                        try
                        {
                            foreach (var group in items.GroupBy(x => (x.Exchange, x.Queue, x.RoutingKey)))
                            {
                                var (exchange, queue, routingKey) = @group.Key;
                                exchange.Declare(channel);
                                queue?.Declare(channel);
                                queue?.Bind(channel, exchange, routingKey);
                                foreach (var item in @group)
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
                            channel.WaitForConfirmsOrDie();
                            channel.Close();
                        }
                    });

                PublishSuccessed?.Invoke(items.AsReadOnly());
            }
            catch (Exception ex)
            {
                _logger?.WriteException(nameof(PublishBufferOnCleared), ex,
                    new KeyValuePair<string, object>("Events", items));
                if (PublishFailed == null)
                {
                    throw;
                }
                PublishFailed.Invoke(items.AsReadOnly(), ex);
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
                _publisherBuffer.Clear();

                while (_consumers.TryTake(out var consumer))
                {
                    consumer.Dispose();
                }
                
                _consumerConnection?.Close();
                _consumerConnection?.Dispose();
                _publisherConnection?.Close();
                _publisherConnection?.Dispose();
            }

            _disposed = true;
        }
    }
}