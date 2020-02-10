using Seedwork.CQRS.Bus.Core;

namespace Seedwork.CQRS.Bus.RabbitMQ
{
    public class BatchItem
    {
        public BatchItem(Exchange exchange, Queue queue, RoutingKey routingKey, IPublishMessage message)
        {
            Exchange = exchange;
            Queue = queue;
            RoutingKey = routingKey;
            Message = message;
        }

        public Exchange Exchange { get; }
        public Queue Queue { get; }
        public RoutingKey RoutingKey { get; }
        public IPublishMessage Message { get; }
    }
}