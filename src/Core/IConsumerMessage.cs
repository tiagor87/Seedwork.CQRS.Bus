using System;
using System.Threading.Tasks;

namespace Seedwork.CQRS.Bus.Core
{
    public interface IPublishMessage
    {
        MessageOptions Options { get; }
        byte[] Body { get; }
        int AttemptCount { get; }
    }
    
    public interface IConsumerMessage : IPublishMessage
    {
        Exception Error { get; }
        Task<T> GetDataAsync<T>();
        T GetData<T>();
        void Success();
        void Fail<T>(T error) where T: Exception;
    }
}