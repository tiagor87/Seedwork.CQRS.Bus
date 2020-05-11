using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RabbitMQ.Client;
using TRDomainDriven.Core;

namespace Seedwork.CQRS.Bus.Core
{
    public class Queue : ValueObject
    {
        private readonly IDictionary<string, object> _arguments = new Dictionary<string, object>();

        private Queue(QueueName name)
        {
            Name = name;
            Durability = Durability.Transient;
            IsAutoDelete = false;
        }

        public QueueName Name { get; }
        public Durability Durability { get; private set; }
        public bool IsAutoDelete { get; private set; }
        public IReadOnlyDictionary<string, object> Arguments => new ReadOnlyDictionary<string, object>(_arguments);

        public static Queue Create(string name)
        {
            return new Queue(QueueName.Create(name));
        }

        /// <summary>
        /// How long a message published to a queue can live before it is discarded.
        /// </summary>
        /// <param name="ttl"></param>
        /// <returns></returns>
        public Queue MessagesExpiresIn(TimeSpan ttl)
        {
            const string key = "x-message-ttl";

            _arguments.Remove(key);
            _arguments.Add(key, (int) ttl.TotalMilliseconds);

            return this;
        }

        public Queue WithDurability(Durability durability)
        {
            Durability = durability;

            return this;
        }

        /// <summary>
        /// The queue will delete itself after at least one consumer has connected, and then all consumers have disconnected. 
        /// </summary>
        /// <returns></returns>
        public Queue WithAutoDelete()
        {
            IsAutoDelete = true;

            return this;
        }

        public Queue WithoutAutoDelete()
        {
            IsAutoDelete = false;

            return this;
        }

        /// <summary>
        /// How long a queue can be unused for before it is automatically deleted.
        /// </summary>
        /// <param name="ttl"></param>
        /// <returns></returns>
        public Queue ExpiresIn(TimeSpan ttl)
        {
            const string key = "x-expires";

            _arguments.Remove(key);
            _arguments.Add(key, (int) ttl.TotalMilliseconds);

            return this;
        }

        /// <summary>
        /// How many ready messages a queue can contain before it starts to drop them from its head.
        /// </summary>
        /// <param name="maxLength"></param>
        /// <param name="behavior">This determines what happens to messages when the maximum length of a queue is reached.</param>
        /// <returns></returns>
        public Queue WithMaxReadyMessages(int maxLength, OverflowMessagesBehavior behavior)
        {
            const string key = "x-max-length";
            const string behaviorKey = "x-overflow";

            _arguments.Remove(key);
            _arguments.Remove(behaviorKey);

            _arguments.Add(key, maxLength);
            _arguments.Add(behaviorKey, behavior.Value);

            return this;
        }

        /// <summary>
        /// Total body size for ready messages a queue can contain before it starts to drop them from its head.
        /// </summary>
        /// <param name="maxTotalMessagesSize"></param>
        /// <returns></returns>
        public Queue WithMaxTotalMessagesSizeInBytes(int maxTotalMessagesSize)
        {
            const string key = "x-max-length-bytes";

            _arguments.Remove(key);
            _arguments.Add(key, maxTotalMessagesSize);

            return this;
        }

        /// <summary>
        /// If messages expires is setup, send the message to another exchange and routing when expired. 
        /// </summary>
        /// <param name="exchangeName">Name of an exchange to which messages will be republished if they are rejected or expire.</param>
        /// <param name="routing">Optional replacement routing key to use when a message is dead-lettered. If this is not set, the message's original routing key will be used.</param>
        /// <returns></returns>
        public Queue SendExpiredMessagesTo(ExchangeName exchangeName, RoutingKey routing)
        {
            const string exchangeKey = "x-dead-letter-exchange";
            const string routingKey = "x-dead-letter-routing-key";

            _arguments.Remove(exchangeKey);
            _arguments.Remove(routingKey);

            _arguments.Add(exchangeKey, exchangeName.Value);
            _arguments.Add(routingKey, routing.Value);

            return this;
        }

        /// <summary>
        /// If messages expires is setup, send the message to another exchange and routing when expired. 
        /// </summary>
        /// <param name="exchange">Exchange to which messages will be republished if they are rejected or expire.</param>
        /// <param name="routing">Optional replacement routing key to use when a message is dead-lettered. If this is not set, the message's original routing key will be used.</param>
        /// <returns></returns>
        public Queue SendExpiredMessagesTo(Exchange exchange, RoutingKey routing)
        {
            return SendExpiredMessagesTo(exchange.Name, routing);
        }

        /// <summary>
        /// Maximum number of priority levels for the queue to support;
        /// if not set, the queue will not support message priorities.
        /// </summary>
        /// <param name="maxPriority"></param>
        /// <returns></returns>
        public Queue WithMaxPriority(int maxPriority)
        {
            const string key = "x-max-priority";

            _arguments.Remove(key);
            _arguments.Add(key, maxPriority);

            return this;
        }

        /// <summary>
        /// Set the queue into lazy mode, keeping as many messages as possible on disk to reduce RAM usage;
        /// if not set or normal mode, the queue will keep an in-memory cache to deliver messages as fast as possible.
        /// </summary>
        /// <param name="queueMode"></param>
        /// <returns></returns>
        public Queue WithMode(QueueMode queueMode)
        {
            const string key = "x-queue-mode";

            _arguments.Remove(key);
            _arguments.Add(key, queueMode.Value);

            return this;
        }

        protected internal void Declare(IModel channel)
        {
            channel.QueueDeclare(Name.Value, Durability.IsDurable, false, IsAutoDelete, _arguments);
        }

        protected internal void Bind(IModel channel, Exchange exchange, RoutingKey routingKey)
        {
            if (exchange.IsDefault) return;
            channel.QueueBind(Name.Value, exchange.Name.Value, routingKey.Value, _arguments);
        }

        protected internal Queue CreateRetryQueue(TimeSpan ttl)
        {
            return Create($"{Name.Value}-retry")
                .WithDurability(Durability.Durable)
                .MessagesExpiresIn(ttl)
                .SendExpiredMessagesTo(Exchange.Default, RoutingKey.Create(Name.Value));
        }

        protected internal Queue CreateFailedQueue()
        {
            return Create($"{Name.Value}-failed")
                .WithDurability(Durability.Durable);
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Name;
        }
    }
}