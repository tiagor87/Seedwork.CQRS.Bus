using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Seedwork.CQRS.Bus.Core.Tests.UnitTests
{
    public class TaskExtensionsTests
    {
        [Fact]
        public void GivenTasksShouldWaitForFreeSlot()
        {
            var tasks = new List<Task>
            {
                Task.Delay(500),
                Task.Delay(100),
                Task.Delay(750),
                Task.Delay(900),
                Task.Delay(1000)
            };
            
            tasks.ExecutionTimeOf(s => s.WaitForFreeSlots(1)).Should().BeCloseTo(TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(200));
        }
    }
}