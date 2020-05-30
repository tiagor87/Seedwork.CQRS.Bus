using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Seedwork.CQRS.Bus.Core.Configurations;
using Seedwork.CQRS.Bus.Core.Tests.UnitTests.Configurations.Stubs;
using Xunit;

namespace Seedwork.CQRS.Bus.Core.Tests.UnitTests.Configurations
{
    public class BusInitializerOptionsBuilderTests
    {
        private const string CONNECTION_STRING = "amqp://guest:guest@localhost/";
        
        [Fact]
        public void GivenBusInitializerOptionsWhenSetRequiredPropertiesShouldBuild()
        {
            var builder = new BusInitializerOptionsBuilder(new ConfigurationBuilder().Build());
            var options = builder
                .SetConnectionString(CONNECTION_STRING)
                .SetSerializer<BusSerializerStub>()
                .Build();

            options.ConnectionString.Should().Be(CONNECTION_STRING);
            options.SerializerImplementationType.Should().Be<BusSerializerStub>();
            options.ConnectionOptions.Should().NotBeNull();
        }
        
        [Fact]
        public void GivenBusInitializerOptionsWhenNotSetConnectionStringShouldThrow()
        {
            var builder = new BusInitializerOptionsBuilder(new ConfigurationBuilder().Build());
            
            Action action = () => builder
                .SetSerializer<BusSerializerStub>()
                .Build();

            action.Should().Throw<ArgumentNullException>();
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void GivenBusInitializerOptionsWhenSetInvalidConnectionStringShouldThrow(string connectionString)
        {
            var builder = new BusInitializerOptionsBuilder(new ConfigurationBuilder().Build());
            
            Action action = () => builder
                .SetConnectionString(connectionString)
                .Build();

            action.Should().Throw<ArgumentException>();
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void GivenBusInitializerOptionsWhenSetInvalidConnectionNameShouldThrow(string name)
        {
            var builder = new BusInitializerOptionsBuilder(new ConfigurationBuilder().Build());
            
            Action action = () => builder
                .SetConnectionName(name)
                .Build();

            action.Should().Throw<ArgumentException>();
        }
        
        [Fact]
        public void GivenBusInitializerOptionsWhenNotSetSerializerTypeShouldThrow()
        {
            var builder = new BusInitializerOptionsBuilder(new ConfigurationBuilder().Build());
            
            Action action = () => builder
                .SetConnectionString(CONNECTION_STRING)
                .Build();

            action.Should().Throw<ArgumentNullException>();
        }
        
        [Fact]
        public void GivenBusInitializerOptionsWhenSetInvalidSerializerTypeShouldThrow()
        {
            var builder = new BusInitializerOptionsBuilder(new ConfigurationBuilder().Build());
            
            Action action = () => builder
                .SetSerializer<AbstractSerializerStub>()
                .Build();

            action.Should().Throw<ArgumentException>();
        }
        
        [Fact]
        public void GivenBusInitializerOptionsWhenSetInvalidConnectionOptionsSectionNameShouldThrow()
        {
            var builder = new BusInitializerOptionsBuilder(new ConfigurationBuilder().Build());
            
            Action action = () => builder
                .SetOptions((string) null)
                .Build();

            action.Should().Throw<ArgumentException>();
        }
        
        [Fact]
        public void GivenBusInitializerOptionsWhenSetInvalidConnectionOptionsShouldThrow()
        {
            var builder = new BusInitializerOptionsBuilder(new ConfigurationBuilder().Build());
            
            Action action = () => builder
                .SetOptions((BusConnectionOptions) null)
                .Build();

            action.Should().Throw<ArgumentException>();
        }
        
        [Fact]
        public void GivenBusInitializerOptionsWhenSetConnectionNameShouldLoadConnectionString()
        {
            const string CONNECTION_NAME = "RabbitMQ";
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>($"ConnectionStrings:{CONNECTION_NAME}",
                    CONNECTION_STRING)
            }).Build();
            
            var builder = new BusInitializerOptionsBuilder(configuration);
            var options = builder
                .SetConnectionName(CONNECTION_NAME)
                .SetSerializer<BusSerializerStub>()
                .Build();

            options.ConnectionString.Should().Be(CONNECTION_STRING);
        }
        
        [Fact]
        public void GivenBusInitializerOptionsWhenIgnoreCertificateShouldSetValidateToFalse()
        {
            var configuration = new ConfigurationBuilder().Build();
            
            var builder = new BusInitializerOptionsBuilder(configuration);
            var options = builder
                .SetConnectionString("amqp://guest:guest@localhost/")
                .IgnoreCertificate()
                .SetSerializer<BusSerializerStub>()
                .Build();

            options.ValidateCertificate.Should().BeFalse();
        }
        
        [Fact]
        public void GivenBusInitializerOptionsWhenSetOptionsShouldLoadOptions()
        {
            var configuration = new ConfigurationBuilder().Build();
            
            var connectionOptions = new BusConnectionOptions
            {
                ConnectionMaxRetry = 5,
                MessageMaxRetry = 5,
                PublisherBufferSize = 5,
                PublishMaxRetry = 5,
                ConsumerMaxParallelTasks = 5,
                ConnectionRetryDelayInMilliseconds = 5,
                PublisherBufferTtlInMilliseconds = 5,
                PublishRetryDelayInMilliseconds = 5
            };
            
            var builder = new BusInitializerOptionsBuilder(configuration);
            var options = builder
                .SetConnectionString(CONNECTION_STRING)
                .SetSerializer<BusSerializerStub>()
                .SetOptions(connectionOptions)
                .Build();

            options.ConnectionOptions.Should().Be(connectionOptions);
        }
        
        [Fact]
        public void GivenBusInitializerOptionsWhenSetOptionsBySectionShouldLoadOptions()
        {
            const string KEY = "ConnectionOptions";
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>($"{KEY}:{nameof(BusConnectionOptions.ConnectionMaxRetry)}",
                    "5"),
                new KeyValuePair<string, string>($"{KEY}:{nameof(BusConnectionOptions.MessageMaxRetry)}",
                    "5"),
                new KeyValuePair<string, string>(
                    $"ConnectionOptions:{nameof(BusConnectionOptions.PublisherBufferSize)}", "5"),
                new KeyValuePair<string, string>($"{KEY}:{nameof(BusConnectionOptions.PublishMaxRetry)}",
                    "5"),
                new KeyValuePair<string, string>(
                    $"{KEY}:{nameof(BusConnectionOptions.ConsumerMaxParallelTasks)}", "5"),
                new KeyValuePair<string, string>(
                    $"{KEY}:{nameof(BusConnectionOptions.ConnectionRetryDelayInMilliseconds)}", "5"),
                new KeyValuePair<string, string>(
                    $"{KEY}:{nameof(BusConnectionOptions.PublisherBufferTtlInMilliseconds)}", "5"),
                new KeyValuePair<string, string>(
                    $"{KEY}:{nameof(BusConnectionOptions.PublishRetryDelayInMilliseconds)}", "5"),
            }).Build();
            
            var connectionOptions = new BusConnectionOptions
            {
                ConnectionMaxRetry = 5,
                MessageMaxRetry = 5,
                PublisherBufferSize = 5,
                PublishMaxRetry = 5,
                ConsumerMaxParallelTasks = 5,
                ConnectionRetryDelayInMilliseconds = 5,
                PublisherBufferTtlInMilliseconds = 5,
                PublishRetryDelayInMilliseconds = 5
            };
            
            var builder = new BusInitializerOptionsBuilder(configuration);
            var options = builder
                .SetConnectionString(CONNECTION_STRING)
                .SetSerializer<BusSerializerStub>()
                .SetOptions(KEY)
                .Build();

            options.Should().NotBeNull();
            options.ConnectionOptions.Should().Be(connectionOptions);
        }
    }
}