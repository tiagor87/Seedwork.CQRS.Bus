using System.Threading.Tasks;
using FluentAssertions;
using Seedwork.CQRS.Bus.Core;
using Seedwork.CQRS.Bus.IntegrationTests.Stubs;
using Xunit;

namespace Seedwork.CQRS.Bus.IntegrationTests
{
    public class DefaultSerializerTests
    {
        [Fact]
        public async Task Should_deserialize()
        {
            var notification = new StubNotification("Message");
            var serializer = new DefaultSerializer();

            var body = await serializer.Serialize(notification);

            var notificationDeserialized = await serializer.Deserialize<StubNotification>(body);

            notificationDeserialized.Message.Should().Be(notification.Message);
        }

        [Fact]
        public async Task Should_serialize()
        {
            var notification = new StubNotification("Message");
            var serializer = new DefaultSerializer();

            var body = await serializer.Serialize(notification);

            body.Length.Should().BeGreaterThan(0);
        }
    }
}