using System;
using Microsoft.Extensions.DependencyInjection;
using Seedwork.CQRS.Bus.Core;

namespace Seedwork.CQRS.Bus.IntegrationTests
{
    public class BusConnectionFixture : IDisposable
    {
        private static volatile object _sync = new object();
        private readonly ServiceProvider _serviceProvider;
        private BusConnection _connection;

        public BusConnectionFixture()
        {
            _serviceProvider = new ServiceCollection()
                .BuildServiceProvider();
        }

        internal BusConnection Connection
        {
            get
            {
                if (_connection != null)
                {
                    return _connection;
                }

                lock (_sync)
                {
                    return _connection ?? (_connection = new BusConnection("amqp://guest:guest@localhost/",
                               new BusSerializer(),
                               _serviceProvider.GetRequiredService<IServiceScopeFactory>()));
                }
            }
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _serviceProvider.Dispose();
        }
    }
}