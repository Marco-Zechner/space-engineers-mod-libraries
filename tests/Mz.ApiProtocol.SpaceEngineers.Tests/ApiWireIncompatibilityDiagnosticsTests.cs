using System;
using System.Collections.Generic;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.SpaceEngineers.Tests
{
    public sealed class ApiWireIncompatibilityDiagnosticsTests
    {
        private const long ChannelId = 918273645L;

        [Fact]
        public void Provider_IncompatibleRequest_RaisesDiagnostic()
        {
            var bus = new InMemoryModMessageBus();

            using var provider = CreateProvider(bus);

            ApiWireIncompatibilityEventArgs observed = null!;

            provider.WireIncompatibilityObserved +=
                delegate(ApiWireIncompatibilityEventArgs eventArgs)
                {
                    observed = eventArgs;
                };

            provider.Start();

            bus.Send(
                ChannelId,
                new object[]
                {
                    ApiDiscoveryWireProtocol.RequestMarker,
                    "2.0.0",
                    "8.1.0",
                    "Mz.FutureConsumer",
                    "Future Consumer",
                    "3.0.0",
                    "Mz.CommandAPI"
                }
            );

            Assert.NotNull(observed);

            Assert.Equal(
                "Mz.FutureConsumer",
                observed.RemoteMod.Id
            );

            Assert.Equal(
                new SemanticVersion(8, 1, 0),
                observed.RemoteLibraryVersion
            );

            Assert.Equal(
                ApiWireCompatibilityStatus.RemoteTooNew,
                observed.CompatibilityStatus
            );

            Assert.Equal(
                ApiWireMessageKind.Request,
                observed.MessageKind
            );
        }

        [Fact]
        public void Consumer_IncompatibleAnnouncement_RaisesDiagnostic()
        {
            var bus = new InMemoryModMessageBus();

            using var consumer = CreateConsumer(bus);

            ApiWireIncompatibilityEventArgs observed = null!;

            consumer.WireIncompatibilityObserved +=
                delegate(ApiWireIncompatibilityEventArgs eventArgs)
                {
                    observed = eventArgs;
                };

            consumer.Start();

            bus.Send(
                ChannelId,
                new object[]
                {
                    ApiDiscoveryWireProtocol.AnnouncementMarker,
                    "2.0.0",
                    "9.0.0",
                    "Mz.FutureProvider",
                    "Future Provider",
                    "5.0.0",
                    "Mz.CommandAPI"
                }
            );

            Assert.NotNull(observed);
            Assert.False(consumer.IsConnected);

            Assert.Equal(
                "Mz.FutureProvider",
                observed.RemoteMod.Id
            );

            Assert.Equal(
                ApiWireCompatibilityStatus.RemoteTooNew,
                observed.CompatibilityStatus
            );

            Assert.Equal(
                ApiWireMessageKind.Announcement,
                observed.MessageKind
            );
        }

        private static ApiDiscoveryProvider CreateProvider(
            IModMessageBus bus
        )
        {
            return new ApiDiscoveryProvider(
                bus,
                ChannelId,
                new ApiModIdentity(
                    "Mz.CommandApiMod",
                    "Command API",
                    new SemanticVersion(1, 4, 0)
                ),
                new ApiDescriptor(
                    "Mz.CommandAPI",
                    new SemanticVersion(1, 5, 0)
                ),
                new Dictionary<string, Delegate>()
            );
        }

        private static ApiDiscoveryConsumer CreateConsumer(
            IModMessageBus bus
        )
        {
            return new ApiDiscoveryConsumer(
                bus,
                ChannelId,
                new ApiDependencyDescriptor(
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
                )
            );
        }
    }
}