using System;

namespace Seedwork.CQRS.Bus.Core
{
    public class ExchangeName
    {
        private ExchangeName(string value)
        {
            Value = value;
        }

        public string Value { get; private set; }
        public static ExchangeName Default => new ExchangeName(string.Empty);

        public static ExchangeName Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            return new ExchangeName(name.Trim());
        }
    }
}