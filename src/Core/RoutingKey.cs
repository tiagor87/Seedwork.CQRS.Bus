using System;
using System.Collections.Generic;
using Seedwork.DomainDriven.Core;

namespace Seedwork.CQRS.Bus.Core
{
    public class RoutingKey : ValueObject
    {
        private RoutingKey(string value)
        {
            Value = value;
        }

        public string Value { get; private set; }

        public static RoutingKey Create(string routing)
        {
            if (string.IsNullOrWhiteSpace(routing))
            {
                throw new ArgumentNullException(nameof(routing));
            }

            return new RoutingKey(routing.Trim());
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }
    }
}