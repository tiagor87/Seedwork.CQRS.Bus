using System;

namespace Seedwork.CQRS.Bus.Core
{
    public class BusConnectionString
    {
        private BusConnectionString(Uri value)
        {
            Value = value;
        }

        public Uri Value { get; }

        public static BusConnectionString Create(string connectionString)
        {
            if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            {
                throw new ArgumentException("The connection string is invalid.", nameof(connectionString));
            }

            return new BusConnectionString(uri);
        }
    }
}