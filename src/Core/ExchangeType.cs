using System;

namespace Seedwork.CQRS.Bus.Core
{
    public class ExchangeType
    {
        private ExchangeType(string type)
        {
            Value = type ?? throw new ArgumentNullException(nameof(type));
        }

        public static ExchangeType Fanout => new ExchangeType("fanout");
        public static ExchangeType Topic => new ExchangeType("topic");
        public static ExchangeType Direct => new ExchangeType("direct");
        public static ExchangeType Header => new ExchangeType("header");

        public string Value { get; }
    }
}