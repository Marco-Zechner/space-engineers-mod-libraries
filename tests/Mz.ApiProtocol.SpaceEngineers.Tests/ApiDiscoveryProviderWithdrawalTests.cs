using System;
using System.Collections.Generic;
using Xunit;

namespace Mz.ApiProtocol.SpaceEngineers.Tests
{
    public sealed class ApiDiscoveryProviderWithdrawalTests
    {
        private const long ChannelId = ApiProtocolChannels.Discovery;

        [Fact]
        public void ProviderAnnouncement_ContainsStableInstanceIdentity()
        {
            var bus = new InMemoryModMessageBus();
            var providerInstanceId = Guid.NewGuid();

            using ApiDiscoveryProvider provider = CreateProvider(bus, providerInstanceId);
            provider.Start();

            ApiAnnouncement startup = FindLastAnnouncement(bus);

            Assert.Equal(providerInstanceId, startup.ProviderInstanceId);

            var correlationId = Guid.NewGuid();

            bus.Send(
                ChannelId,
                ApiDiscoveryWireProtocol.CreateRequest(CreateDependency(), correlationId)
            );

            ApiAnnouncement response = FindLastAnnouncement(bus);

            Assert.Equal(providerInstanceId, response.ProviderInstanceId);
            Assert.Equal(correlationId, response.CorrelationId);
        }

        [Fact]
        public void ProviderStop_DisconnectsConnectedConsumer()
        {
            var bus = new InMemoryModMessageBus();

            using ApiDiscoveryProvider provider = CreateProvider(bus, Guid.NewGuid());
            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            ApiDisconnectReason? reason = null;

            consumer.Disconnected += eventArgs => reason = eventArgs.Reason;

            consumer.Start();
            provider.Start();

            Assert.True(consumer.IsConnected);

            provider.Stop();

            Assert.False(consumer.IsConnected);
            Assert.Equal(ApiDisconnectReason.ProviderWithdrawn, reason);
        }

        [Fact]
        public void WithdrawalFromDifferentProvider_IsIgnored()
        {
            var bus = new InMemoryModMessageBus();
            var connectedProviderId = Guid.NewGuid();

            using ApiDiscoveryProvider provider = CreateProvider(bus, connectedProviderId);
            using ApiDiscoveryConsumer consumer = CreateConsumer(bus);
            consumer.Start();
            provider.Start();

            ApiConnection connection = consumer.Connection;

            bus.Send(
                ChannelId,
                ApiDiscoveryWireProtocol.CreateWithdrawal(
                    CreateProviderIdentity(),
                    "Mz.CommandAPI",
                    Guid.NewGuid()
                )
            );

            Assert.True(consumer.IsConnected);
            Assert.Same(connection, consumer.Connection);
        }

        private static ApiDiscoveryProvider CreateProvider(IModMessageBus bus, Guid providerInstanceId) 
            => new(
                bus,
                CreateProviderIdentity(),
                CreateDescriptor(),
                providerInstanceId,
                CreateEndpoints()
            );

        private static ApiDiscoveryConsumer CreateConsumer(IModMessageBus bus) 
            => new(bus, CreateDependency());

        private static ApiDependencyDescriptor CreateDependency()
            => new(
                new ApiModIdentity("Mz.ConsumerMod", "Consumer Mod", new(2, 0, 0)),
                new ApiRequirement("Mz.CommandAPI", new ApiVersionRange(new(1, 0, 0), new(2, 0, 0))),
                ApiDependencyKind.Optional,
                "Adds Command API integration"
            );

        private static ApiAnnouncement FindLastAnnouncement(InMemoryModMessageBus bus)
        {
            for (int index = bus.SentPayloads.Count - 1; index >= 0; index--)
                if (ApiDiscoveryWireProtocol.TryParseAnnouncement(bus.SentPayloads[index], out ApiAnnouncement announcement))
                    return announcement;

            throw new InvalidOperationException("No announcement was sent.");
        }

        private static ApiDescriptor CreateDescriptor() 
            => new("Mz.CommandAPI", new(1, 5, 0));

        private static IDictionary<string, Delegate> CreateEndpoints() 
            => new Dictionary<string, Delegate> { { "Ping", (Action)delegate { } } };


        private static ApiModIdentity CreateProviderIdentity()
            => new("Mz.CommandApiMod", "Command API", new(1, 4, 0));
    }
}