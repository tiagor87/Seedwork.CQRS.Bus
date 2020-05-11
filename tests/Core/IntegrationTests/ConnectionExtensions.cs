namespace Seedwork.CQRS.Bus.Core.Tests.IntegrationTests
{
    public static class ConnectionExtensions
    {
        public static uint MessageCount(this BusConnection connection, Queue queue)
        {
            using (var channel = connection.ConsumerConnection.CreateModel())
            {
                var count = channel.MessageCount(queue.Name.Value);
                channel.Close();
                return count;
            }
        }

        public static Message GetMessage(this BusConnection connection, Queue queue)
        {
            using (var channel = connection.ConsumerConnection.CreateModel())
            {
                var result = channel.BasicGet(queue.Name.Value, false);
                var serializer = new BusSerializer();
                var message = TestMessage<string>.Create(
                    result,
                    serializer,
                    msg => { },
                    (exception, msg) => { });
                channel.Close();
                return message;
            }
        }
    }
}