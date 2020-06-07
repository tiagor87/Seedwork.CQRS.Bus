using System;

namespace Seedwork.CQRS.Bus.Core.RetryBehaviors
{
    public class GeometricProgressionRetryBehavior : IRetryBehavior
    {
        private readonly int _coeficient;
        private readonly int _initialValue;

        public GeometricProgressionRetryBehavior(int coeficient, int initialValue = 1)
        {
            if (coeficient <= 0) throw new ArgumentOutOfRangeException(nameof(coeficient), coeficient, "The coeficient should be greater then zero.");
            if (initialValue <= 0) throw new ArgumentOutOfRangeException(nameof(initialValue), initialValue, "The initial value should be greater then zero.");
            _coeficient = coeficient;
            _initialValue = initialValue;
        }
        
        public bool ShouldRetry(int attemptCount, int maxAttempts)
        {
            return maxAttempts <= 0 || attemptCount < maxAttempts;
        }

        public TimeSpan GetWaitTime(int attemptCount)
        {
            var value = _initialValue * Math.Pow(_coeficient, attemptCount - 1);
            return TimeSpan.FromMinutes(value);
        }
    }
}