using System;
using System.Collections.Generic;
using Mz.ApiProtocol;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.SpaceEngineers.Tests
{
    public sealed class ApiDiscoveryConsumerMultiProviderTests
    {
        private const long ChannelId = 918273645L;

        [Fact]
        public void RequestDiscovery_FirstProviderIncompatibleSecondCompatible_Connects()
        {
            var bus = new InMemoryModMessageBus();

            using var incompatible = CreateProvider(
                bus,
                new SemanticVersion(2, 0, 0)
            );
            using var compatible = CreateProvider(
                bus,
                new SemanticVersion(1, 5, 0)
            );
            using var consumer = CreateConsumer(bus);
            incompatible.Start();
            compatible.Start();
            consumer.Start();

            consumer.RequestDiscovery();

            Assert.True(consumer.IsConnected);

            Assert.Equal(
                new SemanticVersion(1, 5, 0),
                consumer.Connection.Descriptor.Version
            );

            Assert.Equal(
                Guid.Empty,
                consumer.PendingCorrelationId
            );
        }

        [Fact]
        public void CorrelatedIncompatibleResponse_KeepsRequestPending()
        {
            var bus = new InMemoryModMessageBus();

            using var consumer = CreateConsumer(bus);
            consumer.Start();

            var correlationId =
                consumer.RequestDiscovery();

            bus.Send(
                ChannelId,
                CreateAnnouncement(
                    new SemanticVersion(2, 0, 0),
                    correlationId
                )
            );

            Assert.False(consumer.IsConnected);

            Assert.Equal(
                correlationId,
                consumer.PendingCorrelationId
            );

            Assert.Equal(
                ApiCompatibilityStatus.ProviderTooNew,
                consumer.LastCompatibilityStatus
            );
        }

        [Fact]
        public void CorrelatedCompatibleResponse_ResolvesPendingRequest()
        {
            var bus = new InMemoryModMessageBus();

            using var consumer = CreateConsumer(bus);
            consumer.Start();

            var correlationId =
                consumer.RequestDiscovery();

            bus.Send(
                ChannelId,
                CreateAnnouncement(
                    new SemanticVersion(1, 5, 0),
                    correlationId
                )
            );

            Assert.True(consumer.IsConnected);

            Assert.Equal(
                Guid.Empty,
                consumer.PendingCorrelationId
            );
        }

        [Fact]
        public void NewRequestMakesOlderCorrelatedResponseStale()
        {
            var bus = new InMemoryModMessageBus();

            using var consumer = CreateConsumer(bus);
            consumer.Start();

            var olderCorrelationId =
                consumer.RequestDiscovery();

            var newerCorrelationId =
                consumer.RequestDiscovery();

            bus.Send(
                ChannelId,
                CreateAnnouncement(
                    new SemanticVersion(1, 5, 0),
                    olderCorrelationId
                )
            );

            Assert.False(consumer.IsConnected);

            Assert.Equal(
                newerCorrelationId,
                consumer.PendingCorrelationId
            );

            bus.Send(
                ChannelId,
                CreateAnnouncement(
                    new SemanticVersion(1, 5, 0),
                    newerCorrelationId
                )
            );

            Assert.True(consumer.IsConnected);
        }

        [Fact]
        public void ProviderObserved_RaisedForIncompatibleProvider()
        {
            var bus = new InMemoryModMessageBus();

            using var consumer = CreateConsumer(bus);
            ApiProviderObservedEventArgs observed = null!;

            consumer.ProviderObserved +=
                delegate(
                    object sender,
                    ApiProviderObservedEventArgs eventArgs
                )
                {
                    observed = eventArgs;
                };

            consumer.Start();

            bus.Send(
                ChannelId,
                CreateAnnouncement(
                    new SemanticVersion(2, 0, 0),
                    Guid.Empty
                )
            );

            Assert.NotNull(observed);

            Assert.Equal(
                new SemanticVersion(2, 0, 0),
                observed.Descriptor.Version
            );

            Assert.Equal(
                ApiCompatibilityStatus.ProviderTooNew,
                observed.CompatibilityStatus
            );

            Assert.Equal(
                Guid.Empty,
                observed.CorrelationId
            );
        }

        [Fact]
        public void Connected_RaisedOnceForAcceptedProvider()
        {
            var bus = new InMemoryModMessageBus();

            using var consumer = CreateConsumer(bus);
            var eventCount = 0;
            ApiConnectedEventArgs connected = null!;

            consumer.Connected +=
                delegate(
                    object sender,
                    ApiConnectedEventArgs eventArgs
                )
                {
                    eventCount++;
                    connected = eventArgs;
                };

            consumer.Start();

            var announcement = CreateAnnouncement(
                new SemanticVersion(1, 5, 0),
                Guid.Empty
            );

            bus.Send(ChannelId, announcement);
            bus.Send(ChannelId, announcement);

            Assert.Equal(1, eventCount);
            Assert.NotNull(connected);

            Assert.Same(
                consumer.Connection,
                connected.Connection
            );
        }

        [Fact]
        public void ProviderObservedSubscriberFailure_DoesNotPreventConnection()
        {
            var bus = new InMemoryModMessageBus();

            using var consumer = CreateConsumer(bus);
            consumer.ProviderObserved +=
                delegate
                {
                    throw new InvalidOperationException(
                        "Subscriber failed."
                    );
                };

            consumer.Start();

            bus.Send(
                ChannelId,
                CreateAnnouncement(
                    new SemanticVersion(1, 5, 0),
                    Guid.Empty
                )
            );

            Assert.True(consumer.IsConnected);

            Assert.IsType<InvalidOperationException>(
                consumer.LastError
            );
        }

        [Fact]
        public void ConnectedSubscriberFailure_DoesNotUndoConnection()
        {
            var bus = new InMemoryModMessageBus();

            using var consumer = CreateConsumer(bus);
            consumer.Connected +=
                delegate
                {
                    throw new InvalidOperationException(
                        "Subscriber failed."
                    );
                };

            consumer.Start();

            bus.Send(
                ChannelId,
                CreateAnnouncement(
                    new SemanticVersion(1, 5, 0),
                    Guid.Empty
                )
            );

            Assert.True(consumer.IsConnected);

            Assert.IsType<InvalidOperationException>(
                consumer.LastError
            );
        }

        private static ApiDiscoveryConsumer CreateConsumer(
            IModMessageBus bus
        )
        {
            return new ApiDiscoveryConsumer(
                bus,
                ChannelId,
                new ApiRequirement(
                    "Mz.CommandAPI",
                    new ApiVersionRange(
                        new SemanticVersion(1, 2, 0),
                        new SemanticVersion(2, 0, 0)
                    )
                )
            );
        }

        private static ApiDiscoveryProvider CreateProvider(
            IModMessageBus bus,
            SemanticVersion version
        )
        {
            return new ApiDiscoveryProvider(
                bus,
                ChannelId,
                new ApiDescriptor(
                    "Mz.CommandAPI",
                    version
                ),
                CreateEndpoints()
            );
        }

        private static object CreateAnnouncement(
            SemanticVersion version,
            Guid correlationId
        )
        {
            return ApiDiscoveryWireProtocol.CreateAnnouncement(
                new ApiDescriptor(
                    "Mz.CommandAPI",
                    version
                ),
                correlationId,
                CreateEndpoints()
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
    }
}