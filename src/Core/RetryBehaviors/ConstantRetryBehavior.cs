using System;

namespace Seedwork.CQRS.Bus.Core.RetryBehaviors
{
    public class ConstantRetryBehavior : IRetryBehavior
    {
        private readonly int _coeficient;

        public ConstantRetryBehavior(int coeficient)
        {
            if (coeficient <= 0) throw new ArgumentOutOfRangeException(nameof(coeficient), "The coeficient should be greater then zero.");
            _coeficient = coeficient;
        }
        public bool ShouldRetry(int attemptCount, int maxAttempts)
        {
            return maxAttempts <= 0 || attemptCount < maxAttempts;
        }

        public TimeSpan GetWaitTime(int attemptCount)
        {
            return TimeSpan.FromMinutes(_coeficient);
        }
    }
}