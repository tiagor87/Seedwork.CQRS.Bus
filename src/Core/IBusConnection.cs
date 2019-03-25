using System.Threading;
using System.Threading.Tasks;

namespace Seedwork.CQRS.Bus.Core
{
    public interface IBusConnection
    {
        Task Publish<T>(T notification, CancellationToken cancellationToken) where T : IBusNotification;

        Task Subscribe<T>(BusObserver<T> observer);
    }
}