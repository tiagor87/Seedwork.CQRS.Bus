using System;

namespace Seedwork.CQRS.Bus.Core
{
    internal class DelayQueue : Queue
    {
        public DelayQueue(Exchange exchange, string routingKey, TimeSpan delay) : base($"Delay.{Guid.NewGuid()}",
            $"Delay.{routingKey}")
        {
            AddMessageTTL(delay);
            AddMessageTTLExchangeTarget(exchange);
            AddMessageTTLRoutingKeyTarget(routingKey);
            AddTTL(delay.Add(TimeSpan.FromSeconds(1)));
        }
    }
}