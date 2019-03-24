using System;

namespace Seedwork.CQRS.Bus.Core
{
    public class ExchangeType
    {
//        public static ExchangeType Fanout = new ExchangeType("fanout");
        public static ExchangeType Topic = new ExchangeType("topic");
//        public static ExchangeType Direct = new ExchangeType("direct");
//        public static ExchangeType Header = new ExchangeType("header");

        private ExchangeType(string type)
        {
            Value = type ?? throw new ArgumentNullException(nameof(type));
        }

        public string Value { get; }
    }
}