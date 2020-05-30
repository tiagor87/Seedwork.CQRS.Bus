using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Seedwork.CQRS.Bus.Core.Tests.IntegrationTests
{
    public class BusSerializer : IBusSerializer
    {
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