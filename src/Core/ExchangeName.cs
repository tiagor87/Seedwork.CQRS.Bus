using System;
using System.Collections.Generic;
using Seedwork.DomainDriven.Core;

namespace Seedwork.CQRS.Bus.Core
{
    public class ExchangeName : ValueObject
    {
        private ExchangeName(string value)
        {
            Value = value;
        }

        public string Value { get; private set; }

        public static ExchangeName Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            return new ExchangeName(name.Trim());
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }
    }
}