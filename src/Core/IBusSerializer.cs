namespace Seedwork.CQRS.Bus.Core
{
    public interface IBusSerializer
    {
        T Deserialize<T>(byte[] data);
        byte[] Serialize<T>(T obj);
    }
}