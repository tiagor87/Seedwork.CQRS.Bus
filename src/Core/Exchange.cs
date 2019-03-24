using System;

namespace Seedwork.CQRS.Bus.Core
{
    public abstract class Exchange
    {
        protected Exchange(string name, ExchangeType type, bool durable)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type.Value;
            Durable = durable;
        }

        public string Name { get; }
        public string Type { get; }
        public bool Durable { get; }
    }
}