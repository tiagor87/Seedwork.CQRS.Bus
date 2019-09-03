using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Seedwork.DomainDriven.Core;

namespace Seedwork.CQRS.Bus.Core
{
    public class QueueName : ValueObject
    {
        private QueueName(string value)
        {
            Value = value;
        }

        public string Value { get; private set; }

        public static QueueName Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!Regex.IsMatch(name.Trim(), @"^[\w](\.(?!\.|$)|[\w]|\-(?!$|\-))+$", RegexOptions.None,
                    TimeSpan.FromMilliseconds(10))
                || name.Split('.').Length == 1)
            {
                throw new ArgumentException(
                    "The queue name is invalid. It should follow the rule \"[application-name].[specification-1]<.<specification-n>>\".",
                    nameof(name));
            }

            return new QueueName(name.Trim());
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }
    }
}