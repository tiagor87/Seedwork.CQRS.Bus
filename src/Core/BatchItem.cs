namespace Seedwork.CQRS.Bus.Core
{
    public class BatchItem
    {
        public BatchItem(Exchange exchange, Queue queue, RoutingKey routingKey, Message message)
        {
            Exchange = exchange;
            Queue = queue;
            RoutingKey = routingKey;
            Message = message;
        }

        public Exchange Exchange { get; }
        public Queue Queue { get; }
        public RoutingKey RoutingKey { get; }
        public Message Message { get; }
    }
}