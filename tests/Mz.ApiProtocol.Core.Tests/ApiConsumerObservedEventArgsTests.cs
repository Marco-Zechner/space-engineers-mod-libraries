using System;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiConsumerObservedEventArgsTests
    {
        [Fact]
        public void Constructor_ExposesConsumerRequestMetadata()
        {
            ApiDiscoveryRequest request = CreateRequest();

            var eventArgs = new ApiConsumerObservedEventArgs(request, ApiCompatibilityStatus.ProviderTooNew);

            Assert.Same(request, eventArgs.Request);
            Assert.Same(request.Dependency, eventArgs.Dependency);
            Assert.Same(request.Dependency.Consumer, eventArgs.Consumer);
            Assert.Equal(ApiCompatibilityStatus.ProviderTooNew, eventArgs.CompatibilityStatus);
            Assert.Equal(request.WireProtocolVersion, eventArgs.ConsumerWireProtocolVersion);
            Assert.Equal(request.LibraryVersion, eventArgs.ConsumerLibraryVersion);
            Assert.Equal(request.CorrelationId, eventArgs.CorrelationId);
        }

        [Fact]
        public void Constructor_NullRequest_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                {
                    new ApiConsumerObservedEventArgs(null!, ApiCompatibilityStatus.Compatible);
                }
            );
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        [InlineData(100)]
        public void Constructor_InvalidStatus_ThrowsArgumentException(int numericStatus)
        {
            Assert.Throws<ArgumentException>(() =>
                {
                    new ApiConsumerObservedEventArgs(CreateRequest(), (ApiCompatibilityStatus)numericStatus);
                }
            );
        }

        private static ApiDiscoveryRequest CreateRequest() => new(
            new ApiDependencyDescriptor(
                new ApiModIdentity("Mz.ConsumerMod", "Consumer Mod", new(2, 0, 0)),
                new ApiRequirement("Mz.CommandAPI", new(new(1, 0, 0), new(2, 0, 0))),
                ApiDependencyKind.Optional,
                "Adds command integration"
            ),
            Guid.NewGuid()
        );
    }
}