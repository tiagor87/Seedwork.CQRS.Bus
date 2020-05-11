using System.Collections.Generic;
using TRDomainDriven.Core;

namespace Seedwork.CQRS.Bus.Core
{
    public class QueueMode : ValueObject
    {
        private QueueMode(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static QueueMode Lazy => new QueueMode("lazy");
        public static QueueMode Normal => new QueueMode(string.Empty);
        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }
    }
}