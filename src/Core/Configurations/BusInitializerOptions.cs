using System;

namespace Seedwork.CQRS.Bus.Core.Configurations
{
    public sealed class BusInitializerOptions
    {
        public BusInitializerOptions()
        {
            ConnectionOptionsSectionName = "BusConnectionOptions";
        }

        internal BusConnectionOptions ConnectionOptions { get; private set; }
        internal string ConnectionOptionsSectionName { get; private set; }
        internal string ConnectionString { get; private set; }
        internal Type SerializerImplementationType { get; private set; }

        public BusInitializerOptions SetConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Invalid connection string.", nameof(connectionString));
            }

            ConnectionString = connectionString;
            return this;
        }

        public BusInitializerOptions SetOptions(string sectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
            {
                throw new ArgumentException("Invalid section name.", nameof(sectionName));
            }

            ConnectionOptionsSectionName = sectionName;
            return this;
        }

        public BusInitializerOptions SetOptions(BusConnectionOptions options)
        {
            ConnectionOptions = options;
            return this;
        }

        public BusInitializerOptions SetSerializer<T>()
            where T : class, IBusSerializer
        {
            if (typeof(T).IsAbstract)
            {
                throw new ArgumentException("Serializer cannot be abstract");
            }

            SerializerImplementationType = typeof(T);
            return this;
        }
    }
}