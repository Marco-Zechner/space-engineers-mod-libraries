using System;
using Mz.SemanticVersioning;
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

            Guid correlationId = Guid.NewGuid();

            var request = new ApiDiscoveryRequest(
                dependency,
                correlationId
            );

            Assert.Same(
                dependency,
                request.Dependency
            );

            Assert.Equal(
                "Mz.CommandAPI",
                request.ApiId
            );

            Assert.Equal(
                correlationId,
                request.CorrelationId
            );

            Assert.Equal(
                ApiProtocolInfo.WireProtocolVersion,
                request.WireProtocolVersion
            );

            Assert.Equal(
                ApiProtocolInfo.LibraryVersion,
                request.LibraryVersion
            );
        }

        [Fact]
        public void Constructor_NullDependency_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    new ApiDiscoveryRequest(
                        null!,
                        Guid.NewGuid()
                    );
                }
            );
        }

        [Fact]
        public void Constructor_EmptyCorrelationId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(
                delegate
                {
                    new ApiDiscoveryRequest(
                        CreateDependency(),
                        Guid.Empty
                    );
                }
            );
        }

        private static ApiDependencyDescriptor CreateDependency()
        {
            return new ApiDependencyDescriptor(
                new ApiModIdentity(
                    "Mz.ConsumerMod",
                    "Consumer Mod",
                    new SemanticVersion(2, 0, 0)
                ),
                new ApiRequirement(
                    "Mz.CommandAPI",
                    new ApiVersionRange(
                        new SemanticVersion(1, 0, 0),
                        new SemanticVersion(2, 0, 0)
                    )
                ),
                ApiDependencyKind.Optional,
                "Adds Command API integration"
            );
        }
    }
}