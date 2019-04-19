using System;

namespace Seedwork.CQRS.Bus.Core
{
    public class Exchange
    {
        protected Exchange(string name, ExchangeType type, bool durable = true, bool autoDelete = false)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type.Value;
            Durable = durable;
            AutoDelete = autoDelete;
        }

        public string Name { get; }
        public string Type { get; }
        public bool Durable { get; }
        public bool AutoDelete { get; }
    }
}