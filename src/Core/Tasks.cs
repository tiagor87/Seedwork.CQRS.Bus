using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Seedwork.CQRS.Bus.Core
{
    public class Tasks : IEnumerable<Task>, IDisposable
    {
        private bool _disposed;
        private readonly int _capacity;
        private readonly ConcurrentDictionary<int, Task> _tasks = new ConcurrentDictionary<int, Task>();
        private int _isRunningCount;

        public Tasks(int capacity)
        {
            _capacity = capacity;
        }

        public void Add(Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }
            
            if (task.Status != TaskStatus.Created)
            {
                throw new InvalidOperationException("It's not possible to control a started task.");
            }
            
            WaitForFreeSlots().ConfigureAwait(false).GetAwaiter().GetResult();
            _tasks.TryAdd(task.Id, task);

            Interlocked.Increment(ref _isRunningCount);
            task.ContinueWith(t =>
            {
                _tasks.TryRemove(t.Id, out _);
                Interlocked.Decrement(ref _isRunningCount);
            });
            task.Start();
        }
        public async Task WaitForFreeSlots()
        {
            while (_capacity <= _isRunningCount)
            {
                await Task.Delay(100);
            }
        }

        ~Tasks()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                Task.WaitAll(_tasks.Values.ToArray(), TimeSpan.FromSeconds(10));
                _tasks.Clear();
            }

            _disposed = true;
        }

        public IEnumerator<Task> GetEnumerator()
        {
            return _tasks.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}