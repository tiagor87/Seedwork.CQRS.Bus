using System;

namespace Seedwork.CQRS.Bus.Core
{
    public class BusExecutionException : Exception
    {
        public BusExecutionException(object value, Exception exception) : base("The was an error executing message.",
            exception)
        {
            Value = value;
        }

        public object Value { get; }
    }
}