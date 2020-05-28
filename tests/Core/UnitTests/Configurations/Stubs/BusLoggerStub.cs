using System;
using System.Collections.Generic;

namespace Seedwork.CQRS.Bus.Core.Tests.UnitTests.Configurations.Stubs
{
    internal class BusLoggerStub : IBusLogger
    {
        public void WriteException(string name, Exception exception, params KeyValuePair<string, object>[] properties)
        {
            throw new InvalidOperationException();
        }
    }
}