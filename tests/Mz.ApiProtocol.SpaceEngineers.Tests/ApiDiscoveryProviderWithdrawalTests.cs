using System;
using System.Collections.Generic;
using Mz.ApiProtocol;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.SpaceEngineers.Tests
{
    public sealed class ApiDiscoveryProviderWithdrawalTests
    {
        private const long ChannelId = 918273645L;

        [Fact]
        public void ProviderAnnouncement_ContainsStableInstanceIdentity()
        {
            var bus = new InMemoryModMessageBus();
            Guid providerInstanceId = Guid.NewGuid();

            using var provider = CreateProvider(
                bus,
                providerInstanceId
            );
            provider.Start();

            ApiAnnouncement startup =
                FindLastAnnouncement(bus);

            Assert.Equal(
                providerInstanceId,
                startup.ProviderInstanceId
            );

            Guid correlationId = Guid.NewGuid();

            bus.Send(
                ChannelId,
                ApiDiscoveryWireProtocol.CreateRequest(
                    CreateDependency(),
                    correlationId
                )
            );

            ApiAnnouncement response =
                FindLastAnnouncement(bus);

            Assert.Equal(
                providerInstanceId,
                response.ProviderInstanceId
            );

            Assert.Equal(
                correlationId,
                response.CorrelationId
            );
        }

        [Fact]
        public void ProviderStop_DisconnectsConnectedConsumer()
        {
            var bus = new InMemoryModMessageBus();

            using var provider = CreateProvider(
                bus,
                Guid.NewGuid()
            );
            using var consumer = CreateConsumer(bus);
            ApiDisconnectReason? reason = null;

            consumer.Disconnected +=
                delegate(
                    ApiDisconnectedEventArgs eventArgs
                )
                {
                    reason = eventArgs.Reason;
                };

            consumer.Start();
            provider.Start();

            Assert.True(consumer.IsConnected);

            provider.Stop();

            Assert.False(consumer.IsConnected);

            Assert.Equal(
                ApiDisconnectReason.ProviderWithdrawn,
                reason
            );
        }

        [Fact]
        public void WithdrawalFromDifferentProvider_IsIgnored()
        {
            var bus = new InMemoryModMessageBus();
            Guid connectedProviderId = Guid.NewGuid();

            using var provider = CreateProvider(
                bus,
                connectedProviderId
            );
            using var consumer = CreateConsumer(bus);
            consumer.Start();
            provider.Start();

            ApiConnection connection =
                consumer.Connection;

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

        private static ApiDiscoveryProvider CreateProvider(
            IModMessageBus bus,
            Guid providerInstanceId
        )
        {
            return new ApiDiscoveryProvider(
                bus,
                ChannelId,
                CreateProviderIdentity(),
                CreateDescriptor(),
                providerInstanceId,
                CreateEndpoints()
            );
        }

        private static ApiDiscoveryConsumer CreateConsumer(
            IModMessageBus bus
        )
        {
            return new ApiDiscoveryConsumer(
                bus,
                ChannelId,
                CreateDependency()
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

        private static ApiAnnouncement FindLastAnnouncement(
            InMemoryModMessageBus bus
        )
        {
            for (
                int index = bus.SentPayloads.Count - 1;
                index >= 0;
                index--
            )
            {
                if (ApiDiscoveryWireProtocol.TryParseAnnouncement(
                    bus.SentPayloads[index],
                    out ApiAnnouncement announcement
                ))
                {
                    return announcement;
                }
            }

            throw new InvalidOperationException(
                "No announcement was sent."
            );
        }

        private static ApiDescriptor CreateDescriptor()
        {
            return new ApiDescriptor(
                "Mz.CommandAPI",
                new SemanticVersion(1, 5, 0)
            );
        }

        private static IDictionary<string, Delegate>
            CreateEndpoints()
        {
            return new Dictionary<string, Delegate>
            {
                {
                    "Ping",
                    (Action)delegate
                    {
                    }
                }
            };
        }
        
          
        private static ApiModIdentity CreateProviderIdentity()
        {
            return new ApiModIdentity(
                "Mz.CommandApiMod",
                "Command API",
                new SemanticVersion(1, 4, 0)
            );
        }
    }
}