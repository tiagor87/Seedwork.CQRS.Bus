using System;
using System.Threading;
using System.Threading.Tasks;

namespace Seedwork.CQRS.Bus.Core
{
    public interface IBusConnection
    {
        Task Publish(Exchange exchange, string routingKey, TimeSpan delay, object notification,
            CancellationToken cancellationToken);

        Task Publish<T>(T notification, CancellationToken cancellationToken) where T : IBusNotification;
        Task Subscribe<T>(Exchange exchange, Queue queue, IObserver<T> observer);
        Task Subscribe<T>(BusObserver<T> observer);
        Task Unsubscribe<T>(IObserver<T> observer);
    }
}