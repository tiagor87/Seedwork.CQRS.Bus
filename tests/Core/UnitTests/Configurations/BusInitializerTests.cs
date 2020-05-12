using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using Seedwork.CQRS.Bus.Core.Configurations;
using Seedwork.CQRS.Bus.Core.Tests.UnitTests.Configurations.Stubs;
using Xunit;

namespace Seedwork.CQRS.Bus.Core.Tests.UnitTests.Configurations
{
    public class BusInitializerTests
    {
        [Fact]
        public void GivenServiceCollectionWhenConfigurationIsNullShouldThrow()
        {
            Action action = () => new Mock<IServiceCollection>().Object.AddBusCore(null, _ => { });

            action.Should().Throw<ArgumentNullException>();
        }
        
        [Fact]
        public void GivenServiceCollectionWhenConfigureDelegationIsNullShouldThrow()
        {
            Action action = () => new Mock<IServiceCollection>().Object.AddBusCore(new Mock<IConfiguration>().Object, null);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GivenServicesWhenAddBusCoreShouldAddDependencies()
        {
            var sectionMock = new Mock<IConfigurationSection>();
            var configurationMock = new Mock<IConfiguration>();
            IServiceCollection services = new ServiceCollection();
            
            configurationMock.Setup(x => x.GetSection(It.IsAny<string>()))
                .Returns(sectionMock.Object)
                .Verifiable();

            services.AddBusCore(configurationMock.Object, builder =>
            {
                builder
                    .SetConnectionString("amqp://guest:guest@localhost/")
                    .SetSerializer<BusSerializerStub>();
            });

            services.Any(x =>
                    x.ImplementationInstance is BusConnectionString
                    && x.Lifetime == ServiceLifetime.Singleton)
                .Should()
                .BeTrue();
            services.Any(x =>
                    x.ServiceType == typeof(IBusSerializer)
                    && x.ImplementationType == typeof(BusSerializerStub)
                    && x.Lifetime == ServiceLifetime.Singleton)
                .Should()
                .BeTrue();
            services.Any(x =>
                    x.ImplementationType == typeof(BusConnection)
                    && x.Lifetime == ServiceLifetime.Singleton)
                .Should()
                .BeTrue();
            
            var provider = services.BuildServiceProvider();
            var options = provider.GetService<IOptions<BusConnectionOptions>>();
            options.Should().NotBeNull();
            options.Value.Should().NotBeNull();
            
            configurationMock.VerifyAll();
        }
    }
}