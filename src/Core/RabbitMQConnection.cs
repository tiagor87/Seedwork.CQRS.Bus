using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Seedwork.CQRS.Bus.Core
{
    public class RabbitMQConnection : IBusConnection
    {
        private readonly IModel _channel;
        private readonly IDictionary<object, string> _observers;
        private readonly ISerializer _serializer;

        public RabbitMQConnection(string username, string password, string hostName, string virtualHost = "/",
            ISerializer serializer = null)
        {
            var factory = new ConnectionFactory
            {
                UserName = username,
                Password = password,
                HostName = hostName,
                VirtualHost = virtualHost,
                AutomaticRecoveryEnabled = true
            };
            var connection = factory.CreateConnection();
            _channel = connection.CreateModel();
            _serializer = serializer ?? new DefaultSerializer();
            _observers = new ConcurrentDictionary<object, string>();
        }

        public Task Publish(Exchange exchange, string routingKey, TimeSpan delay, object notification,
            CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
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
            return Task.Factory.StartNew(() =>
            {
                DeclareQueue(exchange, queue);

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += (sender, args) =>
                {
                    try
                    {
                        var value = _serializer.Deserialize<T>(args.Body).Result;

                        observer.OnNext(value);
                        _channel.BasicAck(args.DeliveryTag, false);
                    }
                    catch (Exception exception)
                    {
                        observer.OnError(exception);
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
    }
}