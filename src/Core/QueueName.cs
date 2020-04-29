using System;

namespace Seedwork.CQRS.Bus.Core
{
    public class QueueName
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

            return new QueueName(name.Trim());
        }
    }
}