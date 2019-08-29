using System.Collections.Generic;
using Seedwork.DomainDriven.Core;

namespace Seedwork.CQRS.Bus.Core
{
    public class ExchangeType : ValueObject
    {
        private ExchangeType(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static ExchangeType Fanout => new ExchangeType("fanout");
        public static ExchangeType Direct => new ExchangeType("direct");
        public static ExchangeType Headers => new ExchangeType("headers");
        public static ExchangeType Topic => new ExchangeType("topic");

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }
    }
}