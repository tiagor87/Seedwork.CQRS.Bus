using System.Collections.Generic;
using TRDomainDriven.Core;

namespace Seedwork.CQRS.Bus.Core
{
    public class OverflowMessagesBehavior : ValueObject
    {
        private OverflowMessagesBehavior(string value)
        {
            Value = value;
        }

        public string Value { get; private set; }

        public static OverflowMessagesBehavior DropHead => new OverflowMessagesBehavior("drop-head");
        public static OverflowMessagesBehavior RejectPublish => new OverflowMessagesBehavior("reject-publish");
        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }
    }
}