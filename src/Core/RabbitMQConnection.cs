using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Seedwork.CQRS.Bus.Core
{
    public class RabbitMQConnection : IBusConnection
    {
        private readonly IBusLogger _busLogger;
        private readonly ConnectionFactory _connectionFactory;
        private readonly IDictionary<object, string> _observers;
        private readonly ushort _prefetchCount;
        private readonly ISerializer _serializer;
        private IModel _channel;
        private IConnection _connection;

        public RabbitMQConnection(string connectionString,
            ISerializer serializer = null,
            IBusLogger busLogger = null,
            ushort prefetchCount = 10)
        {
            _busLogger = busLogger;
            _prefetchCount = prefetchCount;
            _connectionFactory = new ConnectionFactory() {Uri = new Uri(connectionString)};
            _serializer = serializer ?? new DefaultSerializer();
            _observers = new ConcurrentDictionary<object, string>();
        }

        public RabbitMQConnection(string username, string password, string hostName, string virtualHost = "/",
            ISerializer serializer = null)
            : this($"amqp://{username}:{password}@{hostName}{virtualHost}", serializer)
        {
        }

        public Task Publish(Exchange exchange, string routingKey, TimeSpan delay, object notification,
            CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                Connect().Wait(cancellationToken);
                _channel.ExchangeDeclare(exchange.Name, exchange.Type, exchange.Durable);

                byte[] body = null;
                try
                {
                    body = _serializer.Serialize(notification).Result;
                }
                catch (Exception exception)
                {
                    _busLogger?.WriteException(
                        $"{nameof(RabbitMQConnection)} - Serialization failed",
                        exception,
                        new KeyValuePair<string, object>("Exchange", exchange.Name),
                        new KeyValuePair<string, object>("RoutingKey", routingKey),
                        new KeyValuePair<string, object>("Delay", delay));
                    throw;
                }

                try
                {
                    if (delay == TimeSpan.Zero)
                    {
                        _channel.BasicPublish(exchange.Name, routingKey, false, null, body);

                        _busLogger?.WriteInformation(
                            nameof(RabbitMQConnection),
                            "Delay message published",
                            new KeyValuePair<string, object>("Exchange", exchange.Name),
                            new KeyValuePair<string, object>("RoutingKey", routingKey),
                            new KeyValuePair<string, object>("Delay", delay));
                        return;
                    }

                    var delayQueue = new DelayQueue(exchange, routingKey, delay);
                    DeclareQueue(exchange, delayQueue);

                    _channel.BasicPublish(exchange.Name, delayQueue.RoutingKey, false, null, body);

                    _busLogger?.WriteInformation(
                        nameof(RabbitMQConnection),
                        "Message published",
                        new KeyValuePair<string, object>("Exchange", exchange.Name),
                        new KeyValuePair<string, object>("RoutingKey", routingKey));
                }
                catch (Exception exception)
                {
                    _busLogger?.WriteException(
                        $"{nameof(RabbitMQConnection)} - Publish failed",
                        exception,
                        new KeyValuePair<string, object>("Exchange", exchange.Name),
                        new KeyValuePair<string, object>("RoutingKey", routingKey),
                        new KeyValuePair<string, object>("Delay", delay));
                    throw;
                }
            }, cancellationToken);
        }

        public Task Publish<T>(T notification, CancellationToken cancellationToken) where T : IBusNotification
        {
            return Publish(notification.GetExchange(), notification.GetRoutingKey(), notification.GetDelay(),
                notification, cancellationToken);
        }

        public Task Subscribe<T>(Exchange exchange, Queue queue, IObserver<T> observer)
        {
            return Task.Factory.StartNew(() =>
            {
                Connect().Wait();
                DeclareQueue(exchange, queue);

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += (sender, args) =>
                {
                    T value;
                    try
                    {
                        value = _serializer.Deserialize<T>(args.Body).Result;
                    }
                    catch (Exception exception)
                    {
                        _channel.BasicNack(args.DeliveryTag, false, true);

                        _busLogger?.WriteException(
                            $"{nameof(RabbitMQConnection)} - Deserialization failed",
                            exception,
                            new KeyValuePair<string, object>("Exchange", args.Exchange),
                            new KeyValuePair<string, object>("RoutingKey", args.RoutingKey));
                        observer.OnError(new BusDeserializeException(args.Body, exception));
                        return;
                    }

                    try
                    {
                        observer.OnNext(value);
                        _channel.BasicAck(args.DeliveryTag, false);
                    }
                    catch (Exception exception)
                    {
                        _channel.BasicNack(args.DeliveryTag, false, true);

                        _busLogger?.WriteException(
                            $"{nameof(RabbitMQConnection)} - Event execution failed",
                            exception,
                            new KeyValuePair<string, object>("Exchange", args.Exchange),
                            new KeyValuePair<string, object>("RoutingKey", args.RoutingKey));
                        observer.OnError(new BusExecutionException(value, exception));
                    }
                };
                var consumerTag = _channel.BasicConsume(queue.Name, false, consumer);
                _busLogger?.WriteInformation(
                    nameof(RabbitMQConnection),
                    "Consumer attached",
                    new KeyValuePair<string, object>("Exchange", exchange.Name),
                    new KeyValuePair<string, object>("Queue", queue.Name),
                    new KeyValuePair<string, object>("RoutingKey", queue.RoutingKey),
                    new KeyValuePair<string, object>("ConsumerTag", consumerTag));
                _observers.Add(observer, consumerTag);
            });
        }

        public Task Unsubscribe<T>(IObserver<T> observer)
        {
            return Task.Factory.StartNew(() =>
            {
                if (!_observers.TryGetValue(observer, out var consumerTag))
                {
                    return;
                }

                observer.OnCompleted();
                _channel.BasicCancel(consumerTag);
            });
        }

        public async Task Subscribe<T>(BusObserver<T> observer)
        {
            await Subscribe(observer.GetExchange(), observer.GetQueue(), observer);
            observer.OnDispose += (sender, args) => Unsubscribe(observer).Wait();
        }

        private void DeclareQueue(Exchange exchange, Queue queue)
        {
            try
            {
                _channel.ExchangeDeclare(exchange.Name, exchange.Type, exchange.Durable);
                _channel.QueueDeclare(queue.Name, queue.Durable, queue.Exclusive, queue.AutoDelete, queue.Arguments);
                _channel.QueueBind(queue.Name, exchange.Name, queue.RoutingKey);
            }
            catch (Exception ex)
            {
                _busLogger?.WriteException($"{nameof(RabbitMQConnection)} - queue declare failed", ex);
                throw;
            }
        }

        private async Task Connect()
        {
            try
            {
                if (_connection != null && _connection.IsOpen
                                        && _channel != null && _channel.IsOpen)
                {
                    return;
                }

                _connection?.Dispose();
                _channel?.Dispose();

                _channel = await Policy.Handle<BrokerUnreachableException>()
                    .WaitAndRetryAsync(5, i => TimeSpan.FromSeconds(i))
                    .ExecuteAsync(() =>
                    {
                        _connection = _connectionFactory.CreateConnection();
                        return Task.FromResult(_connection.CreateModel());
                    });
                _channel.BasicQos(0, _prefetchCount, false);
            }
            catch (Exception ex)
            {
                _busLogger?.WriteException(
                    $"{nameof(RabbitMQConnection)} - connection failed",
                    ex,
                    new KeyValuePair<string, object>("Host", _connectionFactory.HostName),
                    new KeyValuePair<string, object>("Port", _connectionFactory.Port),
                    new KeyValuePair<string, object>("Username", _connectionFactory.UserName));
                throw;
            }
        }
    }
}