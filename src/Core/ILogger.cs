using System;
using System.Collections.Generic;

namespace Seedwork.CQRS.Bus.Core
{
    public interface ILogger
    {
        void WriteInformation(string message, params KeyValuePair<string, object>[] properties);
        void WriteException(string message, Exception exception, params KeyValuePair<string, object>[] properties);
    }
}