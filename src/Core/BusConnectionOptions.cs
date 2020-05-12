using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using TRDomainDriven.Core;

namespace Seedwork.CQRS.Bus.Core
{
    public class BusConnectionOptions : ValueObject
    {
        public BusConnectionOptions()
        {
            PublisherBufferSize = 1000;
            PublisherBufferTtlInMilliseconds = 5000;
            ConnectionMaxRetry = 10;
            ConnectionRetryDelayInMilliseconds = 500;
            ConsumerMaxParallelTasks = 500;
            MessageMaxRetry = 5;
            PublishMaxRetry = 5;
            PublishRetryDelayInMilliseconds = 100;
        }

        public int PublisherBufferSize { get; set; }
        public int PublisherBufferTtlInMilliseconds { get; set; }
        public int ConnectionMaxRetry { get; set; }
        public int ConnectionRetryDelayInMilliseconds { get; set; }
        public int ConsumerMaxParallelTasks { get; set; }
        public int MessageMaxRetry { get; set; }
        public int PublishMaxRetry { get; set; }
        public int PublishRetryDelayInMilliseconds { get; set; }

        internal void Bind(BusConnectionOptions options)
        {
            PublisherBufferSize = options
                .PublisherBufferSize;

            PublisherBufferTtlInMilliseconds = options
                .PublisherBufferTtlInMilliseconds;

            ConnectionMaxRetry = options
                .ConnectionMaxRetry;

            ConnectionRetryDelayInMilliseconds = options
                .ConnectionRetryDelayInMilliseconds;

            ConsumerMaxParallelTasks = options
                .ConsumerMaxParallelTasks;

            MessageMaxRetry = options
                .MessageMaxRetry;

            PublishMaxRetry = options
                .PublishMaxRetry;

            PublishRetryDelayInMilliseconds = options
                .PublishRetryDelayInMilliseconds;
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return ConnectionMaxRetry;
            yield return MessageMaxRetry;
            yield return PublisherBufferSize;
            yield return PublishMaxRetry;
            yield return ConsumerMaxParallelTasks;
            yield return ConnectionRetryDelayInMilliseconds;
            yield return PublisherBufferTtlInMilliseconds;
            yield return PublishRetryDelayInMilliseconds;
        }
    }
}