using System.Threading.Tasks;
using ZeroFormatter;

namespace Seedwork.CQRS.Bus.Core
{
    public class DefaultSerializer : ISerializer
    {
        public Task<byte[]> Serialize<T>(T obj)
        {
            return Task.FromResult(ZeroFormatterSerializer.NonGeneric.Serialize(obj.GetType(), obj));
        }

        public Task<T> Deserialize<T>(byte[] data)
        {
            return Task.FromResult(ZeroFormatterSerializer.Deserialize<T>(data));
        }
    }
}