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
            basicProperties.Headers = new Dictionary<string, object>();
            basicProperties.Headers.Add(nameof(AttemptCount), AttemptCount);
            basicProperties.Headers.Add(nameof(MaxAttempts), MaxAttempts);
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
        private readonly Action<Message<T>> _onDone;
        private readonly Action<Exception, Message<T>> _onFail;
        private bool _completed;
        private bool _disposed;
        private object _sync = new object();

        protected Message(
            IModel channel,
            Exchange exchange,
            Queue queue,
            RoutingKey routingKey,
            ulong deliveryTag,
            T data,
            int maxAttempts,
            int attemptCount,
            Action<Message<T>> onDone,
            Action<Exception, Message<T>> onFail) : base(data, maxAttempts, attemptCount)
        {
            Channel = channel;
            Exchange = exchange;
            Queue = queue;
            RoutingKey = routingKey;
            _onDone = onDone;
            _onFail = onFail;
            DeliveryTag = deliveryTag;
            Value = data;
        }

        public T Value { get; }
        public ulong DeliveryTag { get; }

        internal IModel Channel { get; }
        internal Exchange Exchange { get; }
        internal Queue Queue { get; }
        internal RoutingKey RoutingKey { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
            Action<Message<T>> onDone,
            Action<Exception, Message<T>> onFail)
        {
            var data = serializer.Deserialize<T>(@event.Body).GetAwaiter().GetResult();
            object maxAttempts;
            if (@event.BasicProperties.Headers == null ||
                !@event.BasicProperties.Headers.TryGetValue(nameof(MaxAttempts), out maxAttempts))
            {
                maxAttempts = 5;
            }

            object attempts;
            if (@event.BasicProperties.Headers == null ||
                !@event.BasicProperties.Headers.TryGetValue(nameof(AttemptCount), out attempts))
            {
                attempts = 0;
            }

            var attemptCount = (int) attempts;
            return new Message<T>(
                channel,
                exchange,
                queue,
                routingKey,
                @event.DeliveryTag,
                data,
                (int) maxAttempts,
                ++attemptCount,
                onDone,
                onFail);
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