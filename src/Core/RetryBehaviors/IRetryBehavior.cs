using System;

namespace Seedwork.CQRS.Bus.Core.RetryBehaviors
{
    public interface IRetryBehavior
    {
        bool ShouldRetry(int attemptCount, int maxAttempts);
        TimeSpan GetWaitTime(int attemptCount);
    }
}