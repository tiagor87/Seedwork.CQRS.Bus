using System;
using System.Collections.Generic;

namespace Seedwork.CQRS.Bus.Core
{
    public class Queue
    {
        protected Queue(string name, string routingKey, bool durable = true, bool exclusive = false,
            bool autoDelete = false)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            RoutingKey = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
            Durable = durable;
            Exclusive = exclusive;
            AutoDelete = autoDelete;
            Arguments = new Dictionary<string, object>();
        }

        public string Name { get; }
        public string RoutingKey { get; }
        public bool Durable { get; }
        public bool Exclusive { get; }
        public bool AutoDelete { get; }
        public IDictionary<string, object> Arguments { get; }

        protected void AddTTL(TimeSpan delay)
        {
            Arguments.Add("x-expires", (int) delay.TotalMilliseconds);
        }

        protected void AddMessageTTL(TimeSpan delay)
        {
            Arguments.Add("x-message-ttl", (int) delay.TotalMilliseconds);
        }

        protected void AddMessageTTLExchangeTarget(Exchange exchange)
        {
            Arguments.Add("x-dead-letter-exchange", exchange.Name);
        }

        protected void AddMessageTTLRoutingKeyTarget(string routingKey)
        {
            Arguments.Add("x-dead-letter-routing-key", routingKey);
        }
    }
}