using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Seedwork.CQRS.Bus.Core;

namespace Seedwork.CQRS.Bus.RabbitMQ
{
    public interface IRabbitMqConsumerMessage : IConsumerMessage
    {
        IModel Channel { get; }
        BasicDeliverEventArgs Event { get; }
    }

    public class RabbitMqConsumerMessage : ConsumerMessage, IRabbitMqConsumerMessage
    {
        public RabbitMqConsumerMessage(
            MessageOptions options,
            byte[] body,
            int attemptCount,
            Action<IConsumerMessage> onSuccess,
            Action<IConsumerMessage> onRetry,
            Action<IConsumerMessage> onFail,
            IModel channel,
            BasicDeliverEventArgs @event) : base(options, body, attemptCount, onSuccess, onRetry, onFail)
        {
            Channel = channel;
            Event = @event;
        }
        
        public IModel Channel { get; }
        public BasicDeliverEventArgs Event { get; }
    }
}