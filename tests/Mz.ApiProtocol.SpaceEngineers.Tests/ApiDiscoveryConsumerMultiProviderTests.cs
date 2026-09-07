using System;
using System.Collections.Generic;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.SpaceEngineers.Tests
{
    public sealed class ApiDiscoveryConsumerMultiProviderTests
    {
        private const long ChannelId = ApiProtocolChannels.Discovery;

        [Fact]
        public void RequestDiscovery_FirstProviderIncompatibleSecondCompatible_Connects()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryProvider incompatible = CreateProvider(bus, new(2, 0, 0));
            using ApiDiscoveryProvider compatible = CreateProvider(bus, new(1, 5, 0));
            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            incompatible.Start();
            compatible.Start();
            consumer.Start();

            consumer.RequestDiscovery();

            Assert.True(consumer.IsConnected);

            Assert.Equal(new(1, 5, 0), consumer.Connection.Descriptor.Version);
            Assert.Equal(Guid.Empty, consumer.PendingCorrelationId);
        }

        [Fact]
        public void CorrelatedIncompatibleResponse_KeepsRequestPending()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            consumer.Start();

            Guid correlationId = consumer.RequestDiscovery();

            bus.Send(ChannelId, CreateAnnouncement(new(2, 0, 0), correlationId));

            Assert.False(consumer.IsConnected);
            Assert.Equal(correlationId, consumer.PendingCorrelationId);
            Assert.Equal(ApiCompatibilityStatus.ProviderTooNew, consumer.LastCompatibilityStatus);
        }

        [Fact]
        public void CorrelatedCompatibleResponse_ResolvesPendingRequest()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            consumer.Start();

            Guid correlationId = consumer.RequestDiscovery();

            bus.Send(ChannelId, CreateAnnouncement(new(1, 5, 0), correlationId));

            Assert.True(consumer.IsConnected);
            Assert.Equal(Guid.Empty, consumer.PendingCorrelationId);
        }

        [Fact]
        public void NewRequestMakesOlderCorrelatedResponseStale()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            consumer.Start();

            Guid olderCorrelationId = consumer.RequestDiscovery();
            Guid newerCorrelationId = consumer.RequestDiscovery();

            bus.Send(ChannelId, CreateAnnouncement(new(1, 5, 0), olderCorrelationId));

            Assert.False(consumer.IsConnected);
            Assert.Equal(newerCorrelationId, consumer.PendingCorrelationId);

            bus.Send(ChannelId, CreateAnnouncement(new(1, 5, 0), newerCorrelationId));

            Assert.True(consumer.IsConnected);
        }

        [Fact]
        public void ProviderObserved_RaisedForIncompatibleProvider()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            ApiProviderObservedEventArgs observed = null!;

            consumer.ProviderObserved += eventArgs => observed = eventArgs;

            consumer.Start();

            bus.Send(ChannelId, CreateAnnouncement(new(2, 0, 0), Guid.Empty));

            Assert.NotNull(observed);
            Assert.Equal(new(2, 0, 0), observed.Descriptor.Version);
            Assert.Equal(ApiCompatibilityStatus.ProviderTooNew, observed.CompatibilityStatus);
            Assert.Equal(Guid.Empty, observed.CorrelationId);
        }

        [Fact]
        public void Connected_RaisedOnceForAcceptedProvider()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            var eventCount = 0;
            ApiConnectedEventArgs connected = null!;

            consumer.Connected += eventArgs =>
            {
                eventCount++;
                connected = eventArgs;
            };

            consumer.Start();

            object announcement = CreateAnnouncement(new(1, 5, 0), Guid.Empty);

            bus.Send(ChannelId, announcement);
            bus.Send(ChannelId, announcement);

            Assert.Equal(1, eventCount);
            Assert.NotNull(connected);
            Assert.Same(consumer.Connection, connected.Connection);
        }

        [Fact]
        public void ProviderObservedSubscriberFailure_DoesNotPreventConnection()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            consumer.ProviderObserved += _ => throw new InvalidOperationException("Subscriber failed.");

            consumer.Start();

            bus.Send(ChannelId, CreateAnnouncement(new(1, 5, 0), Guid.Empty));

            Assert.True(consumer.IsConnected);
            Assert.IsType<InvalidOperationException>(consumer.LastError);
        }

        [Fact]
        public void ConnectedSubscriberFailure_DoesNotUndoConnection()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            consumer.Connected += _ => throw new InvalidOperationException("Subscriber failed.");

            consumer.Start();

            bus.Send(ChannelId, CreateAnnouncement(new(1, 5, 0), Guid.Empty));

            Assert.True(consumer.IsConnected);
            Assert.IsType<InvalidOperationException>(consumer.LastError);
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
                ApiDependencyKind.Optional, "Adds Command API integration"
            );

        private static ApiDiscoveryProvider CreateProvider(IModMessageBus bus, SemanticVersion version) 
            => new(
                bus,
                CreateProviderIdentity(),
                new("Mz.CommandAPI", version),
                CreateEndpoints()
            );

        private static object CreateAnnouncement(SemanticVersion version, Guid correlationId) 
            => ApiDiscoveryWireProtocol.CreateAnnouncement(
                CreateProviderIdentity(),
                new("Mz.CommandAPI", version),
                Guid.NewGuid(),
                correlationId,
                CreateEndpoints()
            );

        private static IDictionary<string, Delegate> CreateEndpoints() 
            => new Dictionary<string, Delegate> { { "Ping", (Action)delegate { } } };

        private static ApiModIdentity CreateProviderIdentity() 
            => new("Mz.CommandApiMod", "Command API", new(1, 4, 0));
    }
}