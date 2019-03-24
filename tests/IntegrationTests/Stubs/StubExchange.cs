using Seedwork.CQRS.Bus.Core;

namespace Seedwork.CQRS.Bus.IntegrationTests.Stubs
{
    public class StubExchange : Exchange
    {
        public StubExchange() : base("Seedwork.CQRS.Bus", ExchangeType.Topic, true)
        {
        }

        public static Exchange Instance => new StubExchange();
    }
}