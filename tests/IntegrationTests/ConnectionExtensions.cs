using Seedwork.CQRS.Bus.Core;

namespace Seedwork.CQRS.Bus.IntegrationTests
{
    public static class ConnectionExtensions
    {
        public static uint MessageCount(this BusConnection connection, Queue queue)
        {
            using (var channel = connection.Connection.CreateModel())
            {
                var count = channel.MessageCount(queue.Name.Value);
                channel.Close();
                return count;
            }
        }
    }
}