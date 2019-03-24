using System.Threading;
using System.Threading.Tasks;

namespace Seedwork.CQRS.Bus.Core
{
    public interface IBusConnection
    {
        Task Publish(BusNotification notification, CancellationToken cancellationToken);

        Task Subscribe<T>(BusObserver<T> observer);
    }
}