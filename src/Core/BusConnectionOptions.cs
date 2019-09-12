namespace Seedwork.CQRS.Bus.Core
{
    public class BusConnectionOptions
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
    }
}