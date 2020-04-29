namespace Seedwork.CQRS.Bus.Core
{
    public class QueueMode
    {
        private QueueMode(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static QueueMode Lazy => new QueueMode("lazy");
        public static QueueMode Normal => new QueueMode(string.Empty);
    }
}