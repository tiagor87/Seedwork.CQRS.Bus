using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Seedwork.CQRS.Bus.Core;

namespace Seedwork.CQRS.Bus.IntegrationTests
{
    class BusLogger : IBusLogger
    {
        public Task WriteException(string name, Exception exception, params KeyValuePair<string, object>[] properties)
        {
            return Task.CompletedTask;
        }
    }

    public class BusConnectionFixture : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        internal BusConnectionOptions BusOptions = new BusConnectionOptions
        {
            PublisherBufferTtlInMilliseconds = 1
        };

        public BusConnectionFixture()
        {
            _serviceProvider = new ServiceCollection()
                .AddSingleton(BusConnectionString.Create("amqp://guest:guest@localhost/"))
                .AddSingleton<IBusSerializer, BusSerializer>()
                .AddSingleton<IBusLogger, BusLogger>()
                .AddSingleton<BusConnection>()
                .Configure<BusConnectionOptions>(options =>
                {
                    options.PublisherBufferSize = BusOptions.PublisherBufferSize;
                    options.PublisherBufferTtlInMilliseconds = BusOptions.PublisherBufferTtlInMilliseconds;
                    options.ConnectionMaxRetry = BusOptions.ConnectionMaxRetry;
                    options.ConnectionRetryDelayInMilliseconds = BusOptions.ConnectionRetryDelayInMilliseconds;
                    options.ConsumerMaxParallelTasks = BusOptions.ConsumerMaxParallelTasks;
                    options.MessageMaxRetry = BusOptions.MessageMaxRetry;
                    options.PublishMaxRetry = BusOptions.PublishMaxRetry;
                    options.PublishRetryDelayInMilliseconds = BusOptions.PublishRetryDelayInMilliseconds;
                })
                .BuildServiceProvider();
        }

        internal BusConnection Connection => _serviceProvider.GetService<BusConnection>();

        public void Dispose()
        {
            _serviceProvider.Dispose();
        }
    }
}