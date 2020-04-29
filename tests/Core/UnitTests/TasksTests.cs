using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Seedwork.CQRS.Bus.Core.Tests.UnitTests
{
    public class TasksTests
    {
        [Fact]
        public void GivenTasksShouldWaitForFreeSlot()
        {
            var tasks = new Tasks(1)
            {
                Task.Delay(500),
                Task.Delay(100),
                Task.Delay(750),
                Task.Delay(900),
                Task.Delay(950),
                Task.Delay(999),
                Task.Delay(100),
                Task.Delay(10)
            };

            tasks.Add(Task.Delay(1000));

            tasks.ExecutionTimeOf(s => s.WaitForFreeSlots()).Should().BeCloseTo(TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(200));
        }

        [Fact]
        public async Task GivenTasksShouldAddTasks()
        {
            var tasks = new Tasks(500);
            var addTasks = new List<Task>(); 
            for (var i = 0; i < 1000; i++)
            {
                addTasks.Add(Task.Factory.StartNew(() => tasks.Add(Task.Delay(100))));
            }
            await Task.WhenAll(addTasks);
            tasks.Should().HaveCount(1000);
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            tasks.WaitForFreeSlots();
            tasks.Count().Should().Be(0);
        }

        [Fact]
        public void GivenTasksWhenDisposeShouldWaitTasksExecutions()
        {
            var value = 0;
            var tasks = new Tasks(500);
            for (var i = 0; i < 1000; i++)
            {
                tasks.Add(Task.Factory.StartNew(() => Interlocked.Increment(ref value)));
            }
            tasks.Dispose();
            value.Should().Be(1000);
        }

        [Fact]
        public void GivenTasksWhenCallDisposeTwiceShouldNotFail()
        {
            var tasks = new Tasks(0);
            tasks.Dispose();
            tasks.Invoking(t => t.Dispose()).Should().NotThrow();
        }

        [Fact]
        public void GivenTasksShouldAbleToGetEnumerator()
        {
            IEnumerable tasks = new Tasks(0);
            tasks.GetEnumerator().Should().NotBeNull();
        }
    }
}