using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Seedwork.CQRS.Bus.Core;

namespace Seedwork.CQRS.Bus.Tests.IntegrationTests
{
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
                .AddSingleton<BusConnection>()
                .AddSingleton(Options.Create(BusOptions))
                .BuildServiceProvider();
        }

        internal BusConnection Connection => _serviceProvider.GetService<BusConnection>();

        public void Dispose()
        {
            _serviceProvider.Dispose();
        }
    }
}