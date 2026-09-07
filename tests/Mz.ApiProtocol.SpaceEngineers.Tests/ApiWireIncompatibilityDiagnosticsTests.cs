using System;
using System.Collections.Generic;
using Xunit;

namespace Mz.ApiProtocol.SpaceEngineers.Tests
{
    public sealed class ApiWireIncompatibilityDiagnosticsTests
    {
        private const long ChannelId = ApiProtocolChannels.Discovery;

        [Fact]
        public void Provider_IncompatibleRequest_RaisesDiagnostic()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryProvider provider = CreateProvider(bus);

            ApiWireIncompatibilityEventArgs observed = null!;

            provider.WireIncompatibilityObserved += eventArgs => observed = eventArgs;

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

            Assert.Equal("Mz.FutureConsumer", observed.RemoteMod.Id);
            Assert.Equal(new(8, 1, 0), observed.RemoteLibraryVersion);
            Assert.Equal(ApiWireCompatibilityStatus.RemoteTooNew, observed.CompatibilityStatus);
            Assert.Equal(ApiWireMessageKind.Request, observed.MessageKind);
        }

        [Fact]
        public void Consumer_IncompatibleAnnouncement_RaisesDiagnostic()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);

            ApiWireIncompatibilityEventArgs observed = null!;

            consumer.WireIncompatibilityObserved += eventArgs => observed = eventArgs;

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
            Assert.Equal("Mz.FutureProvider", observed.RemoteMod.Id);
            Assert.Equal(ApiWireCompatibilityStatus.RemoteTooNew, observed.CompatibilityStatus);
            Assert.Equal(ApiWireMessageKind.Announcement, observed.MessageKind);
        }

        private static ApiDiscoveryProvider CreateProvider(IModMessageBus bus) 
            => new(
                bus,
                new ApiModIdentity("Mz.CommandApiMod", "Command API", new(1, 4, 0)),
                new ApiDescriptor("Mz.CommandAPI", new(1, 5, 0)),
                new Dictionary<string, Delegate>()
            );

        private static ApiDiscoveryConsumer CreateConsumer(IModMessageBus bus) 
            => new(
                bus,
                new ApiDependencyDescriptor(
                    new ApiModIdentity("Mz.ConsumerMod", "Consumer Mod", new(2, 0, 0)),
                    new ApiRequirement("Mz.CommandAPI", new(new(1, 0, 0), new(2, 0, 0))),
                    ApiDependencyKind.Optional,
                    "Adds Command API integration"
                )
            );
    }
}