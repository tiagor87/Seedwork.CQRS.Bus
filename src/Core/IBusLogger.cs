using System;
using System.Collections.Generic;

namespace Seedwork.CQRS.Bus.Core
{
    public interface IBusLogger
    {
        void WriteInformation(string operation, string message, params KeyValuePair<string, object>[] properties);
        void WriteException(string operation, Exception exception, params KeyValuePair<string, object>[] properties);
    }
}