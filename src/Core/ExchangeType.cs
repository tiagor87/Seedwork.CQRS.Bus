using System;

namespace Seedwork.CQRS.Bus.Core
{
    public class ExchangeType
    {
        private ExchangeType(string type)
        {
            Value = type ?? throw new ArgumentNullException(nameof(type));
        }

        public static ExchangeType Topic => new ExchangeType("topic");

        public string Value { get; }
    }
}