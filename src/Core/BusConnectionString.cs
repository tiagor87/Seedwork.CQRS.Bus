using System;
using System.Collections.Generic;
using TRDomainDriven.Core;

namespace Seedwork.CQRS.Bus.Core
{
    public class BusConnectionString : ValueObject
    {
        private BusConnectionString(Uri value, bool validateCertificate)
        {
            Value = value;
            ValidateCertificate = validateCertificate;
        }

        public Uri Value { get; }
        public bool ValidateCertificate { get; }

        public static BusConnectionString Create(string connectionString, bool validateCertificate = true)
        {
            if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            {
                throw new ArgumentException("The connection string is invalid.", nameof(connectionString));
            }

            return new BusConnectionString(uri, validateCertificate);
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }
    }
}