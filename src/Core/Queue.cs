using System;

namespace Seedwork.CQRS.Bus.Core
{
    public abstract class Queue
    {
        public Queue(string name, string routingKey, bool durable = true, bool exclusive = false,
            bool autoDelete = false)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            RoutingKey = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
            Durable = durable;
            Exclusive = exclusive;
            AutoDelete = autoDelete;
        }

        public string Name { get; }
        public string RoutingKey { get; }
        public bool Durable { get; }
        public bool Exclusive { get; }
        public bool AutoDelete { get; }
    }
}