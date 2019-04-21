using System;
using System.Text;

namespace Seedwork.CQRS.Bus.Core
{
    public class BusDeserializeException : Exception
    {
        public BusDeserializeException(byte[] body, Exception exception) : base(
            $"There was an error deserializing boby.", exception)
        {
            Body = Encoding.UTF8.GetString(body);
        }

        public string Body { get; }
    }
}