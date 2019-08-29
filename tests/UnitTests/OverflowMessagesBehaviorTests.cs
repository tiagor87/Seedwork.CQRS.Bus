using FluentAssertions;
using Seedwork.CQRS.Bus.Core;
using Xunit;

namespace Seedwork.CQRS.Bus.UnitTests
{
    public class OverflowMessagesBehaviorTests
    {
        [Fact]
        public void GivenBehaviorsWhenSameValueShouldBeEquals()
        {
            var behavior1 = OverflowMessagesBehavior.DropHead;
            var behavior2 = OverflowMessagesBehavior.DropHead;

            behavior1.Should().Be(behavior2);
        }

        [Fact]
        public void GivenBehaviorWhenDropHeadShouldValueBeDropHead()
        {
            var behavior = OverflowMessagesBehavior.DropHead;

            behavior.Should().NotBeNull();
            behavior.Value.Should().Be("drop-head");
        }

        [Fact]
        public void GivenBehaviorWhenRejectPublishShouldValueBeRejectPublish()
        {
            var behavior = OverflowMessagesBehavior.RejectPublish;

            behavior.Should().NotBeNull();
            behavior.Value.Should().Be("reject-publish");
        }
    }
}