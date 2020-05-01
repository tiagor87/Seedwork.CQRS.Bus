using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Seedwork.CQRS.Bus.Core
{
    public class Message
    {
        protected Message(object data, int maxAttempts, int attemptCount)
        {
            Data = data;
            MaxAttempts = maxAttempts;
            AttemptCount = attemptCount;
        }

        public int AttemptCount { get; }

        public int MaxAttempts { get; }

        public object Data { get; }

        public static Message Create(object data, int maxAttempts)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (maxAttempts < 0)
            {
                throw new ArgumentException("The max attempts should be greater or equal to zero.");
            }

            return new Message(data, maxAttempts, 0);
        }

        protected internal virtual (byte[], IBasicProperties) GetData(IModel channel, IBusSerializer serializer)
        {
            var body = serializer.Serialize(Data).GetAwaiter().GetResult();
            var basicProperties = channel.CreateBasicProperties();
            basicProperties.Headers = new Dictionary<string, object>
            {
                {nameof(AttemptCount), AttemptCount},
                {nameof(MaxAttempts), MaxAttempts}
            };
            return (body, basicProperties);
        }

        public bool CanRetry()
        {
            return MaxAttempts == 0 || AttemptCount < MaxAttempts;
        }
    }

    internal class ErrorMessage : Message
    {
        private readonly IBasicProperties _basicProperties;

        private ErrorMessage(byte[] data, IBasicProperties basicProperties) : base(data, 0, 0)
        {
            _basicProperties = basicProperties;
        }

        internal static ErrorMessage Create(byte[] data, IBasicProperties basicProperties)
        {
            return new ErrorMessage(data, basicProperties);
        }

        protected internal override (byte[], IBasicProperties) GetData(IModel channel, IBusSerializer serializer)
        {
            return (Data as byte[], _basicProperties);
        }
    }


    public class Message<T> : Message, IDisposable
    {
        private readonly object _sync = new object();
        private bool _completed;
        private bool _disposed;
        private Action<Message<T>> _onDone;
        private Action<Exception, Message<T>> _onFail;

        protected Message(
            ulong deliveryTag,
            T data,
            int maxAttempts,
            int attemptCount) : base(data, maxAttempts, attemptCount)
        {
            DeliveryTag = deliveryTag;
            Value = data;
        }

        public T Value { get; }
        public ulong DeliveryTag { get; }

        internal IModel Channel { get; set; }
        internal Exchange Exchange { get; set; }
        internal Queue Queue { get; set; }
        internal RoutingKey RoutingKey { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected Message<T> SetOnFail(Action<Exception, Message<T>> onFail)
        {
            _onFail = onFail;
            return this;
        }

        protected Message<T> SetOnDone(Action<Message<T>> onDone)
        {
            _onDone = onDone;
            return this;
        }

        protected Message<T> SetChannel(IModel channel)
        {
            Channel = channel;
            return this;
        }

        protected Message<T> SetExchange(Exchange exchange)
        {
            Exchange = exchange;
            return this;
        }

        protected Message<T> SetQueue(Queue queue)
        {
            Queue = queue;
            return this;
        }

        protected Message<T> SetRoutingKey(RoutingKey routingKey)
        {
            RoutingKey = routingKey;
            return this;
        }

        public void Complete()
        {
            lock (_sync)
            {
                if (_completed) return;
                _completed = true;
                _onDone?.Invoke(this);
            }
        }

        public void Fail(Exception exception)
        {
            lock (_sync)
            {
                if (_completed) return;
                _completed = true;
                _onFail?.Invoke(exception, this);
            }
        }

        protected internal static Message<T> Create(
            IModel channel,
            Exchange exchange,
            Queue queue,
            RoutingKey routingKey,
            IBusSerializer serializer,
            BasicDeliverEventArgs @event,
            ValueTuple<Action<Message<T>>, Action<Exception, Message<T>>> actions)
        {
            var data = serializer.Deserialize<T>(@event.Body.ToArray()).GetAwaiter().GetResult();
            if (@event.BasicProperties.Headers == null ||
                !@event.BasicProperties.Headers.TryGetValue(nameof(MaxAttempts), out var maxAttempts))
            {
                maxAttempts = 5;
            }

            if (@event.BasicProperties.Headers == null ||
                !@event.BasicProperties.Headers.TryGetValue(nameof(AttemptCount), out var attempts))
            {
                attempts = 0;
            }

            var (onDone, onFail) = actions;
            var attemptCount = (int) attempts;
            return new Message<T>(
                    @event.DeliveryTag,
                    data,
                    (int) maxAttempts,
                    ++attemptCount)
                .SetOnDone(onDone)
                .SetOnFail(onFail)
                .SetChannel(channel)
                .SetExchange(exchange)
                .SetQueue(queue)
                .SetRoutingKey(routingKey);
        }

        public static implicit operator T(Message<T> message)
        {
            return message.Value;
        }

        ~Message()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                Fail(new ObjectDisposedException(nameof(Message<T>)));
            }

            _disposed = true;
        }
    }
}