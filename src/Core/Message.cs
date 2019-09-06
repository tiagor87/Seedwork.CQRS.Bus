using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public object Data { get; }
        public int MaxAttempts { get; }
        public int AttemptCount { get; }

        public static Message Create(object data, int maxAttempts)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), $"The {nameof(data)} is required.");
            }

            if (maxAttempts < 0)
            {
                throw new ArgumentException($"The {nameof(maxAttempts)} should be greater than or equal to zero.",
                    nameof(maxAttempts));
            }

            return new Message(data, maxAttempts, 0);
        }

        protected internal static Message Create<T>(IBusSerializer serializer, BasicDeliverEventArgs @event)
        {
            var data = serializer.Deserialize<T>(@event.Body).GetAwaiter().GetResult();
            if (!@event.BasicProperties.Headers.TryGetValue(nameof(MaxAttempts), out var maxAttempts))
            {
                maxAttempts = 5;
            }

            if (!@event.BasicProperties.Headers.TryGetValue(nameof(AttemptCount), out var attempts))
            {
                attempts = 0;
            }

            var attemptCount = (int) attempts;
            return new Message(data, (int) maxAttempts, ++attemptCount);
        }

        protected internal async Task<(byte[], IBasicProperties)> GetData(IModel channel, IBusSerializer serializer)
        {
            var body = await serializer.Serialize(Data);
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
}