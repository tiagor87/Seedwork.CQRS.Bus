using MediatR;

namespace Seedwork.CQRS.Bus.Core
{
    public interface IBusNotification : INotification
    {
        Exchange GetExchange();
        string GetRoutingKey();
    }
}