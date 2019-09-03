using System;
using Microsoft.Extensions.DependencyInjection;
using Seedwork.CQRS.Bus.Core;

namespace Seedwork.CQRS.Bus.IntegrationTests
{
    public class BusConnectionFixture : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        public BusConnectionFixture()
        {
            _serviceProvider = new ServiceCollection()
                .AddSingleton(BusConnectionString.Create("amqp://guest:guest@localhost/"))
                .AddSingleton<IBusSerializer, BusSerializer>()
                .AddSingleton<BusConnection>()
                .BuildServiceProvider();
        }

        internal BusConnection Connection => _serviceProvider.GetService<BusConnection>();

        public void Dispose()
        {
            _serviceProvider.Dispose();
        }
    }
}