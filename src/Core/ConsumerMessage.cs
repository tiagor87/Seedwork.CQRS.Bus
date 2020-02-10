using System;
using System.Threading.Tasks;

namespace Seedwork.CQRS.Bus.Core
{
    public sealed class PublishMessage : IPublishMessage
    {
        private readonly object _data;

        public PublishMessage(MessageOptions options, object data, int attemptCount)
        {
            _data = data;
            Options = options;
            AttemptCount = attemptCount;
        }
        
        public PublishMessage(MessageOptions options, object data) : this (options, data, 0)
        {
        }

        public MessageOptions Options { get; }
        public byte[] Body => Options.Serializer.Serialize(_data);
        public int AttemptCount { get; }
    }
    
    public sealed class ConsumerMessage : IConsumerMessage
    {
        private readonly Action<IConsumerMessage> _onSuccess;
        private readonly Action<IConsumerMessage> _onRetry;
        private readonly Action<IConsumerMessage> _onFail;

        public ConsumerMessage(
            MessageOptions options,
            byte[] body,
            int attemptCount,
            Action<IConsumerMessage> onSuccess,
            Action<IConsumerMessage> onRetry,
            Action<IConsumerMessage> onFail)
        {
            Body = body;
            _onSuccess = onSuccess;
            _onRetry = onRetry;
            _onFail = onFail;
            Options = options;
            AttemptCount = attemptCount;
            
        }

        public MessageOptions Options { get; }
        public byte[] Body { get; }
        public Exception Error { get; private set; }
        public int AttemptCount { get; }
        public async Task<T> GetDataAsync<T>() => await Options.Serializer.DeserializeAsync<T>(Body);
        public T GetData<T>() => Options.Serializer.Deserialize<T>(Body);
        public void Success()
        {
            _onSuccess(this);
        }
        public void Fail<T>(T error) where T: Exception
        {
            Error = error;
            if (AttemptCount < Options.MaxAttempts)
            {
                _onRetry(new RetryConsumerMessage(this));
                return;
            }
            _onFail(this);
        }
    }
    
    public class RetryConsumerMessage : IConsumerMessage
    {
        private readonly IConsumerMessage _consumerMessage;

        public RetryConsumerMessage(IConsumerMessage consumerMessage)
        {
            _consumerMessage = consumerMessage;
        }

        public MessageOptions Options => _consumerMessage.Options;
        public byte[] Body => _consumerMessage.Body;
        public int AttemptCount => _consumerMessage.AttemptCount + 1;
        public Exception Error => _consumerMessage.Error;
        public async Task<T> GetDataAsync<T>() => await _consumerMessage.GetDataAsync<T>();
        public T GetData<T>() => Options.Serializer.Deserialize<T>(Body);
        public void Success() => _consumerMessage.Success();
        public void Fail<T>(T error) where T : Exception => _consumerMessage.Fail(error);
    }
}