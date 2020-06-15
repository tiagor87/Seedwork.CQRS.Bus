using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Seedwork.CQRS.Bus.Core
{
    public interface IBusConnection : IDisposable
    {
        void Declare(Exchange exchange, Queue queue, params RoutingKey[] routingKeys);

        public void Subscribe<T>(Exchange exchange, Queue queue, RoutingKey routingKey, ushort prefetchCount,
                                 Func<IServiceScope, Message<T>, Task> action, bool autoAck = true);

        void Publish(Exchange exchange, Queue queue, RoutingKey routingKey, Message message);

        void Publish(Exchange exchange, Queue queue, RoutingKey routingKey, object data);

        void Publish(Exchange exchange, RoutingKey routingKey, object notification);

        void PublishBatch(Exchange exchange, Queue queue, RoutingKey routingKey,
                          IEnumerable<object> notification);

        void PublishBatch(Exchange exchange, Queue queue, RoutingKey routingKey,
                          IEnumerable<Message> messages);

        event PublishSuccessed PublishSuccessed;

        event PublishFailed PublishFailed;

        IConnection PublisherConnection { get; }

        IConnection ConsumerConnection { get; }

        new void Dispose();

        void Flush();
    }
}
