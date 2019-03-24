using Seedwork.CQRS.Bus.Core;

namespace Seedwork.CQRS.Bus.IntegrationTests.Utils
{
    public class RabbitMQConnectionFactory
    {
        public IBusConnection Create()
        {
            return new RabbitMQConnection("guest", "guest", "localhost");
        }
    }
}