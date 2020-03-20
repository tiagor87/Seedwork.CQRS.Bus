using System;
using System.Threading.Tasks;

namespace Seedwork.CQRS.Bus.Core
{
    public interface IPublishMessage
    {
        MessageOptions Options { get; }
        int AttemptCount { get; }
        byte[] GetBody();
        byte[] GetBody(IBusSerializer serializer);
    }
    
    public interface IConsumerMessage
    {
        MessageOptions Options { get; }
        int AttemptCount { get; }
        byte[] Body { get; }
        Exception Error { get; }
        Task<T> GetDataAsync<T>();
        T GetData<T>();
        void Success();
        void Fail<T>(T error) where T: Exception;
    }
}