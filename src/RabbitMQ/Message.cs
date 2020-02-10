using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Seedwork.CQRS.Bus.Core;

namespace Seedwork.CQRS.Bus.RabbitMQ
{
    public interface IRabbitMqConsumerMessage : IConsumerMessage
    {
        IModel Channel { get; }
        BasicDeliverEventArgs Event { get; }
        bool CanRetry();
    }

    public class RabbitMqConsumerMessage : IRabbitMqConsumerMessage
    {
        private readonly IConsumerMessage _message;

        public RabbitMqConsumerMessage(IConsumerMessage message, IModel channel, BasicDeliverEventArgs @event)
        {
            _message = message;
            Channel = channel;
            Event = @event;
        }

        public MessageOptions Options => _message.Options;
        public byte[] Body => _message.Body;
        public int AttemptCount => _message.AttemptCount;
        public async Task<T> GetDataAsync<T>() => await _message.GetDataAsync<T>();
        public T GetData<T>() => _message.GetData<T>();

        public void Success() => _message.Success();
        public void Fail<T>(T error) where T: Exception
        {
            _message.Fail(error);
        }

        public Exception Error => _message.Error;
        public IModel Channel { get; }
        public BasicDeliverEventArgs Event { get; }
        public bool CanRetry() => _message.AttemptCount < _message.Options.MaxAttempts;
    }
}