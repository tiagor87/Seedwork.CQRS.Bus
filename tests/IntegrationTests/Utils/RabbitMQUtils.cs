using System.Text;
using System.Threading;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Seedwork.CQRS.Bus.Core;
using Seedwork.CQRS.Bus.IntegrationTests.Stubs;

namespace Seedwork.CQRS.Bus.IntegrationTests.Utils
{
    public class RabbitMQUtils
    {
        private readonly IModel _channel;
        private readonly IConnection _connection;

        public RabbitMQUtils()
        {
            var factory = new ConnectionFactory
            {
                UserName = "guest",
                Password = "guest",
                HostName = "localhost",
                VirtualHost = "/",
                AutomaticRecoveryEnabled = true
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        public void Publish(Exchange exchange, string routingKey, object message)
        {
            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            _channel.BasicPublish(exchange.Name, routingKey, false, null, body);
        }

        public void Purge(Queue queue)
        {
            _channel.QueueDeclare(queue.Name, queue.Durable, queue.Exclusive, queue.AutoDelete);
            _channel.QueueBind(queue.Name, StubExchange.Instance.Name, queue.RoutingKey);

            _channel.QueuePurge(queue.Name);
        }

        public uint MessageCount(Queue queue)
        {
            _channel.QueueDeclare(queue.Name, queue.Durable, queue.Exclusive, queue.AutoDelete);
            _channel.QueueBind(queue.Name, StubExchange.Instance.Name, queue.RoutingKey);

            return _channel.MessageCount(queue.Name);
        }

        public void Flush()
        {
            Thread.Sleep(100);
        }
    }
}