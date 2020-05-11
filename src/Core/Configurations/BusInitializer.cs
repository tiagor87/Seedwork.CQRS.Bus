using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Seedwork.CQRS.Bus.Core.Configurations
{
    public static class BusInitializer
    {
        public static IServiceCollection AddBusCore(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<BusInitializerOptions> configure)
        {
            var options = new BusInitializerOptions();

            configure(options);

            services
                .AddSingleton(BusConnectionString.Create(options.ConnectionString))
                .AddSingleton(typeof(IBusSerializer), options.SerializerImplementationType)
                .AddSingleton<BusConnection>();

            ConfigureConnectionOptions(services, configuration, options);

            return services;
        }

        public static IServiceCollection AddBusCore(
            this IServiceCollection services,
            Action<BusInitializerOptions> configure)
        {
            var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetService<IConfiguration>();

            return AddBusCore(services, configuration, configure);
        }

        private static void ConfigureConnectionOptions(IServiceCollection services, IConfiguration configuration,
            BusInitializerOptions options)
        {
            if (options.ConnectionOptions == null)
            {
                services
                    .Configure<BusConnectionOptions>(
                        configuration.GetSection(options.ConnectionOptionsSectionName));
            }
            else
            {
                services
                    .Configure<BusConnectionOptions>(
                        connectionOptions => { FillConnectionOptionProperties(options, connectionOptions); });
            }
        }

        private static void FillConnectionOptionProperties(
            BusInitializerOptions options,
            BusConnectionOptions connectionOptions)
        {
            connectionOptions.PublisherBufferSize = options
                .ConnectionOptions
                .PublisherBufferSize;

            connectionOptions.PublisherBufferTtlInMilliseconds = options
                .ConnectionOptions
                .PublisherBufferTtlInMilliseconds;

            connectionOptions.ConnectionMaxRetry = options
                .ConnectionOptions
                .ConnectionMaxRetry;

            connectionOptions.ConnectionRetryDelayInMilliseconds = options
                .ConnectionOptions
                .ConnectionRetryDelayInMilliseconds;

            connectionOptions.ConsumerMaxParallelTasks = options
                .ConnectionOptions
                .ConsumerMaxParallelTasks;

            connectionOptions.MessageMaxRetry = options
                .ConnectionOptions
                .MessageMaxRetry;

            connectionOptions.PublishMaxRetry = options
                .ConnectionOptions
                .PublishMaxRetry;

            connectionOptions.PublishRetryDelayInMilliseconds = options
                .ConnectionOptions
                .PublishRetryDelayInMilliseconds;
        }
    }
}