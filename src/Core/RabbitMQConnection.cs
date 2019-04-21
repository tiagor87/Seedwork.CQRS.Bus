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
        private readonly ConnectionFactory _connectionFactory;
        private readonly IDictionary<object, string> _observers;
        private readonly ISerializer _serializer;
        private IModel _channel;

        public RabbitMQConnection(string connectionString,
            ISerializer serializer = null)
        {
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
            return Task.Factory.StartNew(async () =>
            {
                await Connect();
                _channel.ExchangeDeclare(exchange.Name, exchange.Type, exchange.Durable);

                var body = _serializer.Serialize(notification).Result;

                if (delay == TimeSpan.Zero)
                {
                    _channel.BasicPublish(exchange.Name, routingKey, false, null, body);
                    return;
                }

                var delayQueue = new DelayQueue(exchange, routingKey, delay);
                DeclareQueue(exchange, delayQueue);

                _channel.BasicPublish(exchange.Name, delayQueue.RoutingKey, false, null, body);
            }, cancellationToken);
        }

        public Task Publish<T>(T notification, CancellationToken cancellationToken) where T : IBusNotification
        {
            return Publish(notification.GetExchange(), notification.GetRoutingKey(), notification.GetDelay(),
                notification, cancellationToken);
        }

        public Task Subscribe<T>(Exchange exchange, Queue queue, IObserver<T> observer)
        {
            return Task.Factory.StartNew(async () =>
            {
                await Connect();
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
                        observer.OnError(new BusDeserializeException(args.Body, exception));
                        _channel.BasicNack(args.DeliveryTag, false, true);
                        return;
                    }

                    try
                    {
                        observer.OnNext(value);
                        _channel.BasicAck(args.DeliveryTag, false);
                    }
                    catch (Exception exception)
                    {
                        observer.OnError(new BusExecutionException(value, exception));
                        _channel.BasicNack(args.DeliveryTag, false, true);
                    }
                };
                var consumerTag = _channel.BasicConsume(queue.Name, false, consumer);
                _observers.Add(observer, consumerTag);
            });
        }

        public Task Unsubscribe<T>(IObserver<T> observer)
        {
            return Task.Factory.StartNew(() =>
            {
                if (_observers.TryGetValue(observer, out var consumerTag))
                {
                    observer.OnCompleted();
                    _channel.BasicCancel(consumerTag);
                }
            });
        }

        public async Task Subscribe<T>(BusObserver<T> observer)
        {
            await Subscribe(observer.GetExchange(), observer.GetQueue(), observer);
            observer.OnDispose += (sender, args) => Unsubscribe(observer).Wait();
        }

        private void DeclareQueue(Exchange exchange, Queue queue)
        {
            _channel.ExchangeDeclare(exchange.Name, exchange.Type, exchange.Durable);
            _channel.QueueDeclare(queue.Name, queue.Durable, queue.Exclusive, queue.AutoDelete, queue.Arguments);
            _channel.QueueBind(queue.Name, exchange.Name, queue.RoutingKey);
        }

        private async Task Connect()
        {
            _channel = _channel ?? await Policy.Handle<BrokerUnreachableException>()
                           .WaitAndRetryAsync(5, i => TimeSpan.FromSeconds(i))
                           .ExecuteAsync(() =>
                           {
                               var connection = _connectionFactory.CreateConnection();
                               return Task.FromResult(connection.CreateModel());
                           });
        }
    }
}