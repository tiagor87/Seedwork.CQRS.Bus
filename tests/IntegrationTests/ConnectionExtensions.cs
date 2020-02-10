using RabbitMQ.Client.Events;
using Seedwork.CQRS.Bus.Core;
using Seedwork.CQRS.Bus.RabbitMQ;

namespace Seedwork.CQRS.Bus.Tests.IntegrationTests
{
    public static class ConnectionExtensions
    {
        public static uint MessageCount(this BusConnection connection, Queue queue)
        {
            using (var channel = connection.ConsumerConnection.CreateModel())
            {
                var count = channel.MessageCount(queue.Name.Value);
                channel.Close();
                return count;
            }
        }

        public static IConsumerMessage GetMessage(this BusConnection connection, Queue queue)
        {
            using (var channel = connection.ConsumerConnection.CreateModel())
            {
                var result = channel.BasicGet(queue.Name.Value, false);
                var serializer = new BusSerializer();
                var @event = new BasicDeliverEventArgs(
                    string.Empty,
                    result.DeliveryTag,
                    result.Redelivered,
                    result.Exchange,
                    result.RoutingKey,
                    result.BasicProperties,
                    result.Body);
                var builder = new MessageBuilder(null, serializer);
                var message = builder.SetEvent(@event)
                    .Build();
                channel.Close();
                return message;
            }
        }
    }
}