using System.Diagnostics.CodeAnalysis;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Seedwork.CQRS.Bus.Core;

namespace Seedwork.CQRS.Bus.IntegrationTests
{
    [ExcludeFromCodeCoverage]
    public class TestMessage : Message
    {
        public TestMessage(object data, int maxAttempts, int attemptCount) : base(data, maxAttempts,
            attemptCount)
        {
        }

        internal static Message Create<T>(IBusSerializer serializer, BasicGetResult result) => Create<T>(serializer,
            new BasicDeliverEventArgs(
                string.Empty,
                result.DeliveryTag,
                result.Redelivered,
                result.Exchange,
                result.RoutingKey,
                result.BasicProperties,
                result.Body));
    }
}