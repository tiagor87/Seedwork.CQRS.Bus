namespace Seedwork.CQRS.Bus.Core
{
    public class Durability
    {
        private Durability(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public bool IsDurable => this == Durable;

        public static Durability Durable => new Durability(nameof(Durable));
        public static Durability Transient => new Durability(nameof(Transient));
    }
}