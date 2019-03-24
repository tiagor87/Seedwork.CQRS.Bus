using Seedwork.CQRS.Bus.Core;

namespace Seedwork.CQRS.Bus.IntegrationTests.Stubs
{
    public class StubNotification : BusNotification
    {
        public StubNotification(string message) : base(StubExchange.Instance, "Seedwork.CQRS.Bus.Queue")
        {
            Message = message;
        }

        public string Message { get; }
    }
}