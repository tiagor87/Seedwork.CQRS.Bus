using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Seedwork.CQRS.Bus.Core
{
    public class RabbitMQConnection : IBusConnection
    {
        private readonly IModel _channel;
        private readonly IConnection _connection;
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
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _serializer = new DefaultSerializer();
        }

        public Task Publish<T>(T notification, CancellationToken cancellationToken) where T : IBusNotification
        {
            return Task.Factory.StartNew(() =>
            {
                var exchange = notification.GetExchange();
                _channel.ExchangeDeclare(exchange.Name, exchange.Type, exchange.Durable);

                var body = _serializer.Serialize(notification).Result;

                var delay = notification.GetDelay();
                if (delay == TimeSpan.Zero)
                {
                    _channel.BasicPublish(exchange.Name, notification.GetRoutingKey(), false, null, body);
                    return;
                }

                var delayQueue = new DelayQueue(exchange, notification.GetRoutingKey(), notification.GetDelay());
                DeclareQueue(exchange, delayQueue);

                _channel.BasicPublish(exchange.Name, delayQueue.RoutingKey, false, null, body);
            }, cancellationToken);
        }

        public Task Subscribe<T>(BusObserver<T> observer)
        {
            return Task.Factory.StartNew(() =>
            {
                var exchange = observer.GetExchange();
                var queue = observer.GetQueue();

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
                var consumerId = _channel.BasicConsume(queue.Name, false, consumer);
                observer.OnDispose += (sender, events) =>
                {
                    observer.OnCompleted();
                    _channel.BasicCancel(consumerId);
                };
            });
        }

        private void DeclareQueue(Exchange exchange, Queue queue)
        {
            _channel.ExchangeDeclare(exchange.Name, exchange.Type, exchange.Durable);
            _channel.QueueDeclare(queue.Name, queue.Durable, queue.Exclusive, queue.AutoDelete, queue.Arguments);
            _channel.QueueBind(queue.Name, exchange.Name, queue.RoutingKey);
        }
    }
}