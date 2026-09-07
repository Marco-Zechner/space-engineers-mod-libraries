using System;
using System.Collections.Generic;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.SpaceEngineers.Tests
{
    public sealed class ApiDiscoveryConsumerTests
    {
        private const long ChannelId = ApiProtocolChannels.Discovery;

        [Fact]
        public void ProviderFirst_RequestDiscoveryConnectsConsumer()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryProvider provider = CreateProvider(bus, new(1, 5, 0));
            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            provider.Start();
            consumer.Start();

            Guid correlationId = consumer.RequestDiscovery();

            Assert.NotEqual(Guid.Empty, correlationId);
            Assert.True(consumer.IsConnected);
            Assert.Equal("Mz.CommandAPI", consumer.Connection.Descriptor.ApiId);
            Assert.Equal(new(1, 5, 0), consumer.Connection.Descriptor.Version);
        }

        [Fact]
        public void ConsumerFirst_LaterProviderAnnouncementConnectsConsumer()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            using ApiDiscoveryProvider provider = CreateProvider(bus, new(1, 5, 0));
            consumer.Start();
            consumer.RequestDiscovery();

            Assert.False(consumer.IsConnected);

            provider.Start();

            Assert.True(consumer.IsConnected);
            Assert.Equal(Guid.Empty, consumer.PendingCorrelationId);
        }

        [Fact]
        public void IncompatibleProvider_IsObservedButNotConnected()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            using ApiDiscoveryProvider provider = CreateProvider(bus, new(2, 0, 0));
            consumer.Start();
            provider.Start();

            Assert.False(consumer.IsConnected);
            Assert.Equal(ApiCompatibilityStatus.ProviderTooNew, consumer.LastCompatibilityStatus);
            Assert.Equal(new(2, 0, 0), consumer.LastObservedProvider.Version);
        }

        [Fact]
        public void ResponseWithUnknownCorrelation_IsIgnored()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            consumer.Start();

            bus.Send(
                ChannelId,
                ApiDiscoveryWireProtocol.CreateAnnouncement(
                    CreateProviderIdentity(),
                    CreateDescriptor(new(1, 5, 0)),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    CreateEndpoints()
                )
            );

            Assert.False(consumer.IsConnected);
            Assert.Null(consumer.LastObservedProvider);
        }

        [Fact]
        public void UnsolicitedAnnouncement_IsAcceptedWithoutRequest()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            consumer.Start();

            bus.Send(
                ChannelId,
                ApiDiscoveryWireProtocol.CreateAnnouncement(
                    CreateProviderIdentity(),
                    CreateDescriptor(new(1, 5, 0)),
                    Guid.NewGuid(),
                    Guid.Empty,
                    CreateEndpoints()
                )
            );

            Assert.True(consumer.IsConnected);
        }

        [Fact]
        public void DuplicateAnnouncements_DoNotReplaceConnection()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            consumer.Start();

            object? announcement =
                ApiDiscoveryWireProtocol.CreateAnnouncement(
                    CreateProviderIdentity(),
                    CreateDescriptor(new(1, 5, 0)),
                    Guid.NewGuid(),
                    Guid.Empty,
                    CreateEndpoints()
                );

            bus.Send(ChannelId, announcement);

            ApiConnection? firstConnection = consumer.Connection;

            bus.Send(ChannelId, announcement);

            Assert.Same(firstConnection, consumer.Connection);
        }

        [Fact]
        public void RequestDiscovery_BeforeStart_Throws()
        {
            using ApiDiscoveryConsumer consumer = CreateConsumer(new InMemoryModMessageBus());
            Assert.Throws<InvalidOperationException>(() => consumer.RequestDiscovery());
        }

        [Fact]
        public void Stop_ClearsConnectionAndStopsListening()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            consumer.Start();

            var providerInstanceId = Guid.NewGuid();
                
            bus.Send(
                ChannelId,
                ApiDiscoveryWireProtocol.CreateAnnouncement(
                    CreateProviderIdentity(),
                    CreateDescriptor(new(1, 5, 0)),
                    providerInstanceId,
                    Guid.Empty,
                    CreateEndpoints()
                )
            );

            Assert.True(consumer.IsConnected);

            consumer.Stop();

            Assert.False(consumer.IsConnected);
            Assert.False(consumer.IsStarted);

            bus.Send(
                ChannelId,
                ApiDiscoveryWireProtocol.CreateAnnouncement(
                    CreateProviderIdentity(),
                    CreateDescriptor(new(1, 5, 0)),
                    providerInstanceId,
                    Guid.Empty,
                    CreateEndpoints()
                )
            );

            Assert.False(consumer.IsConnected);
        }

        [Fact]
        public void MalformedAnnouncement_IsIgnoredWithoutError()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            consumer.Start();
            bus.Send(ChannelId, "malformed");

            Assert.False(consumer.IsConnected);
            Assert.Null(consumer.LastError);
        }

        private static ApiDiscoveryConsumer CreateConsumer(IModMessageBus bus) 
            => new(bus, CreateDependency());

        private static ApiDependencyDescriptor CreateDependency() 
            => new(
                new ApiModIdentity("Mz.ConsumerMod", "Consumer Mod", new(2, 0, 0)),
                new ApiRequirement(
                    "Mz.CommandAPI",
                    new ApiVersionRange(new(1, 2, 0), new(2, 0, 0))
                ),
                ApiDependencyKind.Optional,
                "Adds Command API integration"
            );

        private static ApiDiscoveryProvider CreateProvider(IModMessageBus bus, SemanticVersion version) 
            => new(bus, CreateProviderIdentity(), CreateDescriptor(version), CreateEndpoints());

        private static ApiDescriptor CreateDescriptor(SemanticVersion version) 
            => new("Mz.CommandAPI", version);

        private static IDictionary<string, Delegate> CreateEndpoints() 
            => new Dictionary<string, Delegate> { { "Ping", (Action)delegate { } } };

        private static ApiModIdentity CreateProviderIdentity() 
            => new("Mz.CommandApiMod", "Command API", new(1, 4, 0));
    }
}