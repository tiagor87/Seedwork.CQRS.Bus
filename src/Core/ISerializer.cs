using System.Threading.Tasks;

namespace Seedwork.CQRS.Bus.Core
{
    public interface ISerializer
    {
        Task<byte[]> Serialize<T>(T obj);
        Task<T> Deserialize<T>(byte[] data);
    }
}