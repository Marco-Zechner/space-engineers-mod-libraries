using System;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiDiscoveryRequestTests
    {
        [Fact]
        public void Constructor_StoresDependencyAndCorrelationId()
        {
            ApiDependencyDescriptor dependency =
                CreateDependency();

            var correlationId = Guid.NewGuid();

            var request = new ApiDiscoveryRequest(dependency, correlationId);

            Assert.Same(dependency, request.Dependency);
            Assert.Equal("Mz.CommandAPI", request.ApiId);
            Assert.Equal(correlationId, request.CorrelationId);
            Assert.Equal(ApiProtocolInfo.WireProtocolVersion, request.WireProtocolVersion);
            Assert.Equal(ApiProtocolInfo.LibraryVersion, request.LibraryVersion);
        }

        [Fact]
        public void Constructor_NullDependency_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                {
                    new ApiDiscoveryRequest(null!, Guid.NewGuid());
                }
            );
        }

        [Fact]
        public void Constructor_EmptyCorrelationId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                {
                    new ApiDiscoveryRequest(CreateDependency(), Guid.Empty);
                }
            );
        }

        private static ApiDependencyDescriptor CreateDependency() => new(
            new ApiModIdentity("Mz.ConsumerMod", "Consumer Mod", new(2, 0, 0)),
            new ApiRequirement("Mz.CommandAPI", new(new(1, 0, 0), new(2, 0, 0))),
            ApiDependencyKind.Optional,
            "Adds Command API integration"
        );
    }
}