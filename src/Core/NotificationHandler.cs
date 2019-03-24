using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Seedwork.CQRS.Bus.Core
{
    public class NotificationHandler : INotificationHandler<BusNotification>
    {
        private readonly IBusConnection _connection;

        public NotificationHandler(IBusConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public Task Handle(BusNotification notification, CancellationToken cancellationToken)
        {
            return _connection.Publish(notification, cancellationToken);
        }
    }
}