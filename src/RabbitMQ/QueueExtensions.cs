using System;
using System.Linq;
using RabbitMQ.Client;
using Seedwork.CQRS.Bus.Core;

namespace Seedwork.CQRS.Bus.RabbitMQ
{
    public static class QueueExtensions
    {
        internal static void Declare(this Queue queue, IModel channel)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            
            var arguments = queue.Arguments.ToDictionary(x => x.Key, x => x.Value);
            channel.QueueDeclare(queue.Name.Value, queue.Durability.IsDurable, false, queue.IsAutoDelete,
                arguments);
        }

        internal static void Bind(this Queue queue, IModel channel, Exchange exchange, RoutingKey routingKey)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (exchange == null) throw new ArgumentNullException(nameof(exchange));
            if (routingKey == null) throw new ArgumentNullException(nameof(routingKey));
            
            if (exchange.IsDefault) return;
            var arguments = queue.Arguments.ToDictionary(x => x.Key, x => x.Value);
            channel.QueueBind(queue.Name.Value, exchange.Name.Value, routingKey.Value, arguments);
        }

        internal static Queue CreateRetryQueue(this Queue queue, TimeSpan ttl)
        {
            return Queue.Create($"{queue.Name.Value}-retry")
                .WithDurability(Durability.Durable)
                .MessagesExpiresIn(ttl)
                .SendExpiredMessagesTo(Exchange.Default, RoutingKey.Create(queue.Name.Value));
        }

        internal static Queue CreateFailedQueue(this Queue queue)
        {
            return Queue.Create($"{queue.Name.Value}-failed")
                .WithDurability(Durability.Durable);
        }
    }
}