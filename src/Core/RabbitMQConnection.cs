using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Seedwork.CQRS.Bus.Core
{
    public class RabbitMQConnection : IBusConnection
    {
        private readonly IModel _channel;
        private readonly IConnection _connection;

        public RabbitMQConnection(string username, string password, string hostName, string virtualHost = "/")
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
        }

        public Task Publish(BusNotification notification, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                var exchange = notification.GetExchange();
                _channel.ExchangeDeclare(exchange.Name, exchange.Type, exchange.Durable);

                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(notification));

                _channel.BasicPublish(exchange.Name, notification.GetRoutingKey(), false, null, body);
            }, cancellationToken);
        }

        public Task Subscribe<T>(BusObserver<T> observer)
        {
            return Task.Factory.StartNew(() =>
            {
                var exchange = observer.GetExchange();
                var queue = observer.GetQueue();

                _channel.ExchangeDeclare(exchange.Name, exchange.Type, exchange.Durable);
                _channel.QueueDeclare(queue.Name, queue.Durable, queue.Exclusive, queue.AutoDelete);
                _channel.QueueBind(queue.Name, exchange.Name, queue.RoutingKey);

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += (sender, args) =>
                {
                    try
                    {
                        var value = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(args.Body));

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
    }
}