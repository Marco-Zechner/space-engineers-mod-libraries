using System;
using System.Collections.Generic;
using Xunit;

namespace Mz.ApiProtocol.SpaceEngineers.Tests
{
    public sealed class ApiDiscoveryConsumerRediscoveryTests
    {
        private const long ChannelId = ApiProtocolChannels.Discovery;

        [Fact]
        public void Disconnect_RemovesConnectionAndRaisesEvent()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            ApiDisconnectedEventArgs disconnected = null!;

            consumer.Disconnected += eventArgs => disconnected = eventArgs;

            consumer.Start();
            AnnounceCompatibleProvider(bus);

            ApiConnection? previous = consumer.Connection;

            bool removed = consumer.Disconnect();

            Assert.True(removed);
            Assert.False(consumer.IsConnected);
            Assert.NotNull(disconnected);
            Assert.Same(previous, disconnected.PreviousConnection);
            Assert.Equal(ApiDisconnectReason.ConsumerRequested, disconnected.Reason);
        }

        [Fact]
        public void Disconnect_WhenAlreadyDisconnected_ReturnsFalse()
        {
            using ApiDiscoveryConsumer consumer = CreateConsumer(new InMemoryModMessageBus());
            consumer.Start();

            Assert.False(consumer.Disconnect());
        }

        [Fact]
        public void RequestDiscovery_WhileConnected_Throws()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            consumer.Start();
            AnnounceCompatibleProvider(bus);

            Assert.Throws<InvalidOperationException>(() => consumer.RequestDiscovery());
        }

        [Fact]
        public void Rediscover_RemovesConnectionAndSendsNewRequest()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            ApiDisconnectReason? reason = null;

            consumer.Disconnected += eventArgs => reason = eventArgs.Reason;

            consumer.Start();
            AnnounceCompatibleProvider(bus);

            int sendCountBeforeRediscovery = bus.SendCount;
            Guid correlationId = consumer.Rediscover();

            Assert.NotEqual(Guid.Empty, correlationId);
            Assert.Equal(sendCountBeforeRediscovery + 1, bus.SendCount);
            Assert.Equal(ApiDisconnectReason.RediscoveryRequested, reason);
            Assert.False(consumer.IsConnected);
            Assert.Equal(correlationId, consumer.PendingCorrelationId);
        }

        [Fact]
        public void Rediscover_WithAvailableProviderReconnectsSynchronously()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryProvider provider = CreateProvider(bus);
            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            provider.Start();
            consumer.Start();
            consumer.RequestDiscovery();

            ApiConnection firstConnection = consumer.Connection;

            Guid correlationId = consumer.Rediscover();

            Assert.NotEqual(Guid.Empty, correlationId);
            Assert.True(consumer.IsConnected);
            Assert.NotSame(firstConnection, consumer.Connection);
            Assert.Equal(Guid.Empty, consumer.PendingCorrelationId);
        }

        [Fact]
        public void Stop_RaisesDisconnectedWithConsumerStoppedReason()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            ApiDisconnectReason? reason = null;

            consumer.Disconnected += eventArgs => reason = eventArgs.Reason;

            consumer.Start();
            AnnounceCompatibleProvider(bus);

            consumer.Stop();

            Assert.Equal(ApiDisconnectReason.ConsumerStopped, reason);
            Assert.False(consumer.IsConnected);
            Assert.False(consumer.IsStarted);
        }

        [Fact]
        public void FailingDisconnectedSubscriber_DoesNotRestoreConnection()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            consumer.Disconnected += _ => throw new InvalidOperationException("Subscriber failed.");

            consumer.Start();
            AnnounceCompatibleProvider(bus);

            consumer.Disconnect();

            Assert.False(consumer.IsConnected);
            Assert.IsType<InvalidOperationException>(consumer.LastError);
        }

        [Fact]
        public void FailingSubscriber_DoesNotPreventLaterSubscriber()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            var laterSubscriberCalled = false;

            consumer.Disconnected += _ => throw new InvalidOperationException("First subscriber failed.");
            consumer.Disconnected += _ => laterSubscriberCalled = true;

            consumer.Start();
            AnnounceCompatibleProvider(bus);

            consumer.Disconnect();

            Assert.True(laterSubscriberCalled);
        }

        private static void AnnounceCompatibleProvider(IModMessageBus bus)
        {
            bus.Send(
                ChannelId,
                ApiDiscoveryWireProtocol.CreateAnnouncement(
                    CreateProviderIdentity(),
                    new("Mz.CommandAPI", new(1, 5, 0)),
                    Guid.NewGuid(),
                    Guid.Empty,
                    CreateEndpoints()
                )
            );
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

        private static ApiDiscoveryProvider CreateProvider(IModMessageBus bus) 
            => new(
                bus,
                CreateProviderIdentity(),
                new ApiDescriptor("Mz.CommandAPI", new(1, 5, 0)),
                CreateEndpoints()
            );

        private static IDictionary<string, Delegate> CreateEndpoints() 
            => new Dictionary<string, Delegate> { { "Ping", (Action)delegate { } } };

        private static ApiModIdentity CreateProviderIdentity() 
            => new("Mz.CommandApiMod", "Command API", new(1, 4, 0));
    }
}