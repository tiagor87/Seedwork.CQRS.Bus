using System;
using System.Collections.Generic;
using Seedwork.DomainDriven.Core;

namespace Seedwork.CQRS.Bus.Core
{
    public class BusConnectionString : ValueObject
    {
        private BusConnectionString(Uri value)
        {
            Value = value;
        }

        public Uri Value { get; }

        public static BusConnectionString Create(string connectionString)
        {
            if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            {
                return new BusConnectionString(uri);
            }

            throw new ArgumentException("The connection string is invalid.", nameof(connectionString));
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }
    }
}