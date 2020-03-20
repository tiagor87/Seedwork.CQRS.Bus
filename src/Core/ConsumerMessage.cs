using System;
using System.Threading.Tasks;

namespace Seedwork.CQRS.Bus.Core
{
    public sealed class PublishMessage : IPublishMessage
    {
        private readonly object _data;
        private byte[] _body;

        public PublishMessage(MessageOptions options, object data, int attemptCount)
        {
            _data = data;
            _body = Array.Empty<byte>();
            Options = options;
            AttemptCount = attemptCount;
        }
        
        public PublishMessage(MessageOptions options, object data) : this (options, data, 0)
        {
        }

        public MessageOptions Options { get; }
        public int AttemptCount { get; }
        public byte[] GetBody()
        {
            return GetBody(Options.Serializer);
        }

        public byte[] GetBody(IBusSerializer serializer)
        {
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));
            if (_data is IConsumerMessage consumerMessage) return consumerMessage.Body;
            return _body.Length == 0
                ? _body = serializer.Serialize(_data)
                : _body;
        }
    }
    
    public class ConsumerMessage : IConsumerMessage
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
            if (CanRetry())
            {
                _onRetry(new RetryConsumerMessage(this));
                return;
            }
            _onFail(this);
        }
        
        private bool CanRetry()
        {
            return AttemptCount < Options.MaxAttempts;
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