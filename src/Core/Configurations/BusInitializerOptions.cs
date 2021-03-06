﻿using System;
using Seedwork.CQRS.Bus.Core.RetryBehaviors;

namespace Seedwork.CQRS.Bus.Core.Configurations
{
    public class BusInitializerOptions
    {
        public BusInitializerOptions(
            string connectionString,
            bool validateCertificate,
            BusConnectionOptions connectionOptions,
            Type serializerImplementationType,
            Type loggerImplementationType,
            IRetryBehavior retryBehavior)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            ValidateCertificate = validateCertificate;
            ConnectionOptions = connectionOptions ?? throw new ArgumentNullException(nameof(connectionOptions));
            SerializerImplementationType = serializerImplementationType ??
                                           throw new ArgumentNullException(nameof(serializerImplementationType));
            LoggerImplementationType = loggerImplementationType;
            RetryBehavior = retryBehavior ?? throw new ArgumentNullException(nameof(retryBehavior));
        }

        public BusConnectionOptions ConnectionOptions { get; protected set; }
        public  string ConnectionString { get; protected set; }
        public bool ValidateCertificate { get; protected set; }
        public  Type SerializerImplementationType { get; protected set; }
        public Type LoggerImplementationType { get; protected set; }
        public IRetryBehavior RetryBehavior { get; protected set; }
    }
}