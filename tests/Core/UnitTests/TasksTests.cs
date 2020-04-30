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
            Action action = () =>
            {
                var tasks = new Tasks(1)
                {
                    new Task(() => Task.Delay(100).Wait()),
                    new Task(() => Task.Delay(100).Wait()),
                    new Task(() => Task.Delay(100).Wait()),
                    new Task(() => Task.Delay(100).Wait()),
                    new Task(() => Task.Delay(100).Wait())
                };

                tasks.Add(new Task(() => Task.Delay(1000).Wait()));
            };

            action.ExecutionTimeOf(s => s.Invoke()).Should().BeGreaterThan(TimeSpan.FromMilliseconds(1000));
        }

        [Fact]
        public async Task GivenTasksShouldAddTasks()
        {
            var tasks = new Tasks(5);
            var addTasks = new List<Task>(); 
            for (var i = 0; i < 10; i++)
            {
                addTasks.Add(Task.Factory.StartNew(() => tasks.Add(new Task(() => Task.Delay(100).Wait()))));
            }
            await Task.WhenAll(addTasks);
            await Task.Delay(500);
            tasks.Count().Should().Be(0);
        }

        [Fact]
        public void GivenTasksWhenDisposeShouldWaitTasksExecutions()
        {
            var value = 0;
            var tasks = new Tasks(500);
            for (var i = 0; i < 1000; i++)
            {
                tasks.Add(new Task(() => Interlocked.Increment(ref value)));
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