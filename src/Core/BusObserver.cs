using System;
using System.Diagnostics.CodeAnalysis;

namespace Seedwork.CQRS.Bus.Core
{
    public abstract class BusObserver<T> : IObserver<T>, IDisposable
    {
        private readonly Exchange _exchange;
        private readonly Queue _queue;
        private bool _disposed;

        protected BusObserver(Exchange exchange, Queue queue)
        {
            _exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public abstract void OnCompleted();
        public abstract void OnError(Exception error);
        public abstract void OnNext(T value);

        public Exchange GetExchange() => _exchange;
        public Queue GetQueue() => _queue;

        internal event EventHandler OnDispose;

        [ExcludeFromCodeCoverage]
        ~BusObserver()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // Put objects to dispose here.
            }

            OnDispose?.Invoke(this, null);

            _disposed = true;
        }
    }
}