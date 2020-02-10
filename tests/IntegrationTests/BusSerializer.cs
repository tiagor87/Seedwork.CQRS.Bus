using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Seedwork.CQRS.Bus.Core;

namespace Seedwork.CQRS.Bus.Tests.IntegrationTests
{
    public class BusSerializer : IBusSerializer
    {
        public Task<T> DeserializeAsync<T>(byte[] data)
        {
            return Task.FromResult(Deserialize<T>(data));
        }

        public Task<byte[]> SerializeAsync<T>(T obj)
        {
            return Task.FromResult(Serialize(obj));
        }

        public T Deserialize<T>(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public byte[] Serialize<T>(T obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}