using Seedwork.CQRS.Bus.Core;

namespace Seedwork.CQRS.Bus.IntegrationTests.Stubs
{
    public class StubQueue : Queue
    {
        public StubQueue(string name = "StubQueue", string routingKey = "Seedwork.CQRS.Bus.Queue") : base(name,
            routingKey)
        {
        }

        public static Queue Instance => new StubQueue();
    }
}