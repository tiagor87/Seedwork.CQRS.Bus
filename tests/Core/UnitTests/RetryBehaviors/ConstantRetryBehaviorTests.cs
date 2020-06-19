using System;
using FluentAssertions;
using Seedwork.CQRS.Bus.Core.RetryBehaviors;
using Xunit;

namespace Seedwork.CQRS.Bus.Core.Tests.UnitTests.RetryBehaviors
{
    public class ConstantRetryBehaviorTests
    {
        [Theory]
        [InlineData(1, 1, 1)]
        [InlineData(1, 5, 1)]
        [InlineData(5, 1, 5)]
        [InlineData(2, 2, 2)]
        public void GivenRetryBehaviorWhenAttemptShouldCalculateDelay(
            int coeficient,
            int attempt,
            int delay)
        {
            var behavior = new ConstantRetryBehavior(coeficient);
            
            var timeSpan = behavior.GetWaitTime(attempt);

            timeSpan.Should().Be(TimeSpan.FromMinutes(delay));
        }

        [Theory]
        [InlineData(0, 5, true)]
        [InlineData(1, 5, true)]
        [InlineData(2, 5, true)]
        [InlineData(5, 5, false)]
        [InlineData(9, 10, true)]
        [InlineData(10, 10, false)]
        public void GivenRetryBehaviorShouldVerifyIfCanRetry(int attempt, int maxAttempts, bool shouldRetry)
        {
            var behavior = new ConstantRetryBehavior(1);

            behavior.ShouldRetry(attempt, maxAttempts).Should().Be(shouldRetry);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public void GivenRetryBehaviorWhenInvalidCoeficientShouldThrow(int coeficient)
        {
            Action action = () => new ConstantRetryBehavior(coeficient);

            action.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}