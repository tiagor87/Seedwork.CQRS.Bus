using System;
using System.Collections.Generic;
using Seedwork.DomainDriven.Core;

namespace Seedwork.CQRS.Bus.Core
{
    public class BusConnectionString : ValueObject
    {
        private BusConnectionString(Uri value, uint maxThreads)
        {
            Value = value;
            MaxThreads = maxThreads;
        }

        public Uri Value { get; }
        public uint MaxThreads { get; }

        public static BusConnectionString Create(string connectionString, uint maxTasks = 20)
        {
            if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            {
                throw new ArgumentException("The connection string is invalid.", nameof(connectionString));
            }

            if (maxTasks == 0)
            {
                throw new ArgumentException("The max tasks should be greater or equal than one.", nameof(maxTasks));
            }

            return new BusConnectionString(uri, maxTasks);
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }
    }
}