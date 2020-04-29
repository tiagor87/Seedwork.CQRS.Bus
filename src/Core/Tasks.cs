using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Seedwork.CQRS.Bus.Core
{
    public class Tasks : IEnumerable<Task>, IDisposable
    {
        private bool _disposed;
        private readonly int _capacity;
        private readonly object _sync = new object();
        private List<Task> _tasks = new List<Task>();

        public Tasks(int capacity)
        {
            _capacity = capacity;
        }

        public void Add(Task task)
        {
            lock (_sync)
            {
                _tasks.Add(task);
            }
        }
        public void WaitForFreeSlots()
        {
            int freeSlots;
            lock (_sync)
            {
                freeSlots = _capacity - _tasks.Count;
            }

            while (freeSlots <= 0)
            {
                Task.Delay(100).Wait();
                lock (_sync)
                {
                    _tasks.RemoveAll(x => x.IsCompleted || x.IsCanceled || x.IsFaulted);
                    freeSlots = _capacity - _tasks.Count;
                }
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

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                lock (_sync)
                {
                    Task.WaitAll(_tasks.ToArray(), TimeSpan.FromMilliseconds(100));
                    _tasks.Clear();
                    _tasks = null;
                }
            }

            _disposed = true;
        }

        public IEnumerator<Task> GetEnumerator()
        {
            lock (_sync)
            {
                return _tasks.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}