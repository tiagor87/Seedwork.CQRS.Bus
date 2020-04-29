using System.Collections.Generic;
using System.Threading.Tasks;

namespace Seedwork.CQRS.Bus.Core
{
    public static class TaskExtensions
    {
        private static object _sync = new object(); 
        public static void WaitForFreeSlots(this List<Task> tasks, int maxParallelTasks)
        {
            int freeSlots;
            lock (_sync)
            {
                freeSlots = maxParallelTasks - tasks.Count;
            }

            while (freeSlots <= 0)
            {
                Task.Delay(100).Wait();
                lock (_sync)
                {
                    tasks.RemoveAll(x => x.IsCompleted || x.IsCanceled || x.IsFaulted);
                    freeSlots = maxParallelTasks - tasks.Count;
                }
            }
        }
    }
}