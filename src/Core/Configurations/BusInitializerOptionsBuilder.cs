using System;
using Microsoft.Extensions.Configuration;

namespace Seedwork.CQRS.Bus.Core.Configurations
{
    public class BusInitializerOptionsBuilder
    {
        private readonly IConfiguration _configuration;
        private string _connectionString;
        private Type _serializerImplementationType;
        private BusConnectionOptions _options;

        public BusInitializerOptionsBuilder(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            SetOptions("BusConnectionOptions");
        }

        public BusInitializerOptionsBuilder SetConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Invalid connection string.", nameof(connectionString));
            }

            _connectionString = connectionString;
            return this;
        }
        
        public BusInitializerOptionsBuilder SetConnectionName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Invalid connection string name.", nameof(name));
            }

            _connectionString = _configuration.GetConnectionString(name);
            return this;
        }

        public BusInitializerOptionsBuilder SetOptions(string sectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
            {
                throw new ArgumentException("Invalid section name.", nameof(sectionName));
            }

            var options = new BusConnectionOptions();
            _configuration.GetSection(sectionName).Bind(options);
            return SetOptions(options);
        }

        public BusInitializerOptionsBuilder SetOptions(BusConnectionOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            return this;
        }

        public BusInitializerOptionsBuilder SetSerializer<T>()
            where T : class, IBusSerializer
        {
            if (typeof(T).IsAbstract)
            {
                throw new ArgumentException("Serializer cannot be abstract.");
            }

            _serializerImplementationType = typeof(T);
            return this;
        }

        public BusInitializerOptions Build()
        {
            return new BusInitializerOptions(_connectionString, _options, _serializerImplementationType);
        }
    }
}