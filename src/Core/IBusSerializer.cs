using System.Threading.Tasks;

namespace Seedwork.CQRS.Bus.Core
{
    public interface IBusSerializer
    {
        Task<T> Deserialize<T>(byte[] data);
        Task<byte[]> Serialize<T>(T obj);
    }
}