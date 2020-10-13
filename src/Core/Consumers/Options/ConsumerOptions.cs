using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Seedwork.CQRS.Bus.Core.Consumers.Options
{
    public class ConsumerOptions<T>
    {
        public ConsumerOptions(IBusSerializer serializer, Exchange exchange, Queue queue, ushort prefetchCount, Func<IServiceScope, Message<T>, Task> onNext, bool autoAck, int consumerMaxParallelTasks, params RoutingKey[] routingKeys)
        {
            if (prefetchCount <= 0) throw new ArgumentException(nameof(prefetchCount));
            Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            Exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
            Queue = queue ?? throw new ArgumentNullException(nameof(queue));
            PrefetchCount = prefetchCount;
            OnNext = onNext ?? throw new ArgumentNullException(nameof(onNext));
            AutoAck = autoAck;
            ConsumerMaxParallelTasks = consumerMaxParallelTasks;
            RoutingKeys = routingKeys;
        }

        public bool AutoAck { get; }
        public ushort PrefetchCount { get; }
        public IBusSerializer Serializer { get; }
        public Exchange Exchange { get; }
        public Queue Queue { get; }
        public IReadOnlyCollection<RoutingKey> RoutingKeys { get; }
        public Func<IServiceScope, Message<T>, Task> OnNext { get; }
        public int ConsumerMaxParallelTasks { get; }
    }
}