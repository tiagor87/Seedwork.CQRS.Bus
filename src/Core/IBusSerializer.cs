using System.Threading.Tasks;

namespace Seedwork.CQRS.Bus.Core
{
    public interface IBusSerializer
    {
        Task<T> DeserializeAsync<T>(byte[] data);
        Task<byte[]> SerializeAsync<T>(T obj);
        T Deserialize<T>(byte[] data);
        byte[] Serialize<T>(T obj);
    }
}