using System;
using MediatR;

namespace Seedwork.CQRS.Bus.Core
{
    public abstract class BusNotification : INotification
    {
        private readonly Exchange _exchange;
        private readonly string _routingKey;

        protected BusNotification(Exchange exchange, string routingKey)
        {
            _exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
            _routingKey = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
        }

        public Exchange GetExchange() => _exchange;
        public string GetRoutingKey() => _routingKey;
    }
}