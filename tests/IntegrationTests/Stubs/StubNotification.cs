using System;
using Seedwork.CQRS.Bus.Core;
using ZeroFormatter;

namespace Seedwork.CQRS.Bus.IntegrationTests.Stubs
{
    [ZeroFormattable]
    public class StubNotification : IBusNotification
    {
        private readonly TimeSpan _delay;
        private readonly Exchange _exchange;
        private readonly string _routingKey;

        public StubNotification()
        {
            _exchange = StubExchange.Instance;
            _routingKey = "Seedwork.CQRS.Bus.Queue";
            _delay = TimeSpan.Zero;
        }

        public StubNotification(string message) : this()
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public StubNotification(Exchange exchange, string routingKey, TimeSpan delay, string message)
        {
            _exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
            _routingKey = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
            _delay = delay;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        [Index(0)] public virtual string Message { get; protected set; }

        public Exchange GetExchange() => _exchange;

        public string GetRoutingKey() => _routingKey;
        public TimeSpan GetDelay() => _delay;
    }
}