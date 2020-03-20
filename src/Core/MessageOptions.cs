namespace Seedwork.CQRS.Bus.Core
{
    public class MessageOptions
    {
        public MessageOptions(Exchange exchange, Queue queue, RoutingKey routingKey, int maxAttempts, IBusSerializer serializer)
        {
            Exchange = exchange;
            Queue = queue;
            RoutingKey = routingKey;
            MaxAttempts = maxAttempts;
            Serializer = serializer;
        }
        
        public MessageOptions(Exchange exchange, Queue queue, RoutingKey routingKey, int maxAttempts) : this(exchange, queue, routingKey, maxAttempts, null)
        {
        }
        
        public Exchange Exchange { get; }
        public Queue Queue { get; }
        public int MaxAttempts { get; }
        public IBusSerializer Serializer { get; }
        public RoutingKey RoutingKey { get; }
    }
}