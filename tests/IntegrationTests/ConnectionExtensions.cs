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

        public static Message GetMessage(this BusConnection connection, Queue queue)
        {
            using (var channel = connection.Connection.CreateModel())
            {
                var result = channel.BasicGet(queue.Name.Value, false);
                var serializer = new BusSerializer();
                var message = TestMessage.Create<string>(serializer, result);
                channel.Close();
                return message;
            }
        }
    }
}