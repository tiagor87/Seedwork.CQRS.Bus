using System;
using System.Collections.Generic;

namespace Seedwork.CQRS.Bus.Core
{
    public interface IBusLogger
    {
        void WriteException(string name, Exception exception, params KeyValuePair<string, object>[] properties);
    }
}