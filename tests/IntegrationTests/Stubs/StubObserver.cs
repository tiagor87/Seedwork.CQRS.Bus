using System;
using Seedwork.CQRS.Bus.Core;

namespace Seedwork.CQRS.Bus.IntegrationTests.Stubs
{
    public class StubObserver : BusObserver<StubNotification>
    {
        public StubObserver() : base(StubExchange.Instance, StubQueue.Instance)
        {
        }

        public StubObserver(Queue queue) : base(StubExchange.Instance, queue)
        {
        }

        public bool Completed { get; private set; }
        public Exception Error { get; private set; }
        public StubNotification Value { get; private set; }

        public override void OnCompleted()
        {
            Completed = true;
        }

        public override void OnError(Exception error)
        {
            Error = error;
        }

        public override void OnNext(StubNotification value)
        {
            Value = value;
        }
    }
}