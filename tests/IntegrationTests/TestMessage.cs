using System;
using System.Diagnostics.CodeAnalysis;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Seedwork.CQRS.Bus.Core;

namespace Seedwork.CQRS.Bus.Tests.IntegrationTests
{
    [ExcludeFromCodeCoverage]
    public class TestMessage<T> : Message<T>
    {
        public TestMessage(T data, int maxAttempts, int attemptCount, Action<Message<T>> onDone,
            Action<Exception, Message<T>> onFail) : base(0, data, maxAttempts, attemptCount)
        {
            SetOnDone(onDone);
            SetOnFail(onFail);
        }

        internal static Message<T> Create(
            BasicGetResult result,
            IBusSerializer serializer,
            Action<Message<T>> onDone,
            Action<Exception, Message<T>> onFail)
        {
            var @event = new BasicDeliverEventArgs(
                string.Empty,
                result.DeliveryTag,
                result.Redelivered,
                result.Exchange,
                result.RoutingKey,
                result.BasicProperties,
                result.Body);

            return Create(
                null,
                null,
                null,
                (RoutingKey) null,
                serializer,
                @event,
                onDone,
                onFail);
        }
    }
}