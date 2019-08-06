using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

            if (!Regex.IsMatch(routing.Trim(), @"^[a-z](\.(?!\.|$)|[a-z\*#]|\-(?!$|\-))+$")
                || routing.Split('.').Length == 1)
            {
                throw new ArgumentException(
                    "The routing key is invalid. It should follow the rule \"[application-name].[specification-1]<.<specification-n>>\".",
                    nameof(routing));
            }

            return new RoutingKey(routing.Trim());
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }
    }
}