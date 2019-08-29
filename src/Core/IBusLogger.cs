using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Seedwork.CQRS.Bus.Core
{
    public interface IBusLogger
    {
        Task WriteException(string name, Exception exception, params KeyValuePair<string, object>[] properties);
    }
}