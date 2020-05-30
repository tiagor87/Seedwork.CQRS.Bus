using System;
using System.Threading.Tasks;

namespace Seedwork.CQRS.Bus.Core.Tests.UnitTests.Configurations.Stubs
{
    internal class BusSerializerStub : IBusSerializer
    {
        public T Deserialize<T>(byte[] data)
        {
            throw new InvalidOperationException();
        }

        public byte[] Serialize<T>(T obj)
        {
            throw new InvalidOperationException();
        }
    }
}