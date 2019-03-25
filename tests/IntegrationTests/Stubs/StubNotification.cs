using Seedwork.CQRS.Bus.Core;
using ZeroFormatter;

namespace Seedwork.CQRS.Bus.IntegrationTests.Stubs
{
    [ZeroFormattable]
    public class StubNotification : IBusNotification
    {
        public StubNotification()
        {
        }

        public StubNotification(string message)
        {
            Message = message;
        }

        [Index(0)] public virtual string Message { get; protected set; }

        public Exchange GetExchange() => StubExchange.Instance;

        public string GetRoutingKey() => "Seedwork.CQRS.Bus.Queue";
    }
}