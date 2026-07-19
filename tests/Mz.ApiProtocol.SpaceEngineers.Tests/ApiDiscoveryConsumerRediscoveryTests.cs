using System;
using System.Collections.Generic;
using Mz.ApiProtocol;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.SpaceEngineers.Tests
{
    public sealed class ApiDiscoveryConsumerRediscoveryTests
    {
        private const long ChannelId = 918273645L;

        [Fact]
        public void Disconnect_RemovesConnectionAndRaisesEvent()
        {
            var bus = new InMemoryModMessageBus();

            using var consumer = CreateConsumer(bus);
            ApiDisconnectedEventArgs disconnected = null!;

            consumer.Disconnected +=
                delegate(
                    object sender,
                    ApiDisconnectedEventArgs eventArgs
                )
                {
                    disconnected = eventArgs;
                };

            consumer.Start();
            AnnounceCompatibleProvider(bus);

            var previous = consumer.Connection;

            var removed = consumer.Disconnect();

            Assert.True(removed);
            Assert.False(consumer.IsConnected);
            Assert.NotNull(disconnected);

            Assert.Same(
                previous,
                disconnected.PreviousConnection
            );

            Assert.Equal(
                ApiDisconnectReason.ConsumerRequested,
                disconnected.Reason
            );
        }

        [Fact]
        public void Disconnect_WhenAlreadyDisconnected_ReturnsFalse()
        {
            using var consumer = CreateConsumer(
                new InMemoryModMessageBus()
            );
            consumer.Start();

            Assert.False(consumer.Disconnect());
        }

        [Fact]
        public void RequestDiscovery_WhileConnected_Throws()
        {
            var bus = new InMemoryModMessageBus();

            using var consumer = CreateConsumer(bus);
            consumer.Start();
            AnnounceCompatibleProvider(bus);

            Assert.Throws<InvalidOperationException>(
                delegate
                {
                    consumer.RequestDiscovery();
                }
            );
        }

        [Fact]
        public void Rediscover_RemovesConnectionAndSendsNewRequest()
        {
            var bus = new InMemoryModMessageBus();

            using var consumer = CreateConsumer(bus);
            ApiDisconnectReason? reason = null;

            consumer.Disconnected +=
                delegate(
                    object sender,
                    ApiDisconnectedEventArgs eventArgs
                )
                {
                    reason = eventArgs.Reason;
                };

            consumer.Start();
            AnnounceCompatibleProvider(bus);

            var sendCountBeforeRediscovery = bus.SendCount;

            var correlationId = consumer.Rediscover();

            Assert.NotEqual(Guid.Empty, correlationId);

            Assert.Equal(
                sendCountBeforeRediscovery + 1,
                bus.SendCount
            );

            Assert.Equal(
                ApiDisconnectReason.RediscoveryRequested,
                reason
            );

            Assert.False(consumer.IsConnected);

            Assert.Equal(
                correlationId,
                consumer.PendingCorrelationId
            );
        }

        [Fact]
        public void Rediscover_WithAvailableProviderReconnectsSynchronously()
        {
            var bus = new InMemoryModMessageBus();

            using var provider = CreateProvider(bus);
            using var consumer = CreateConsumer(bus);
            provider.Start();
            consumer.Start();
            consumer.RequestDiscovery();

            var firstConnection =
                consumer.Connection;

            var correlationId = consumer.Rediscover();

            Assert.NotEqual(Guid.Empty, correlationId);
            Assert.True(consumer.IsConnected);

            Assert.NotSame(
                firstConnection,
                consumer.Connection
            );

            Assert.Equal(
                Guid.Empty,
                consumer.PendingCorrelationId
            );
        }

        [Fact]
        public void Stop_RaisesDisconnectedWithConsumerStoppedReason()
        {
            var bus = new InMemoryModMessageBus();

            using var consumer = CreateConsumer(bus);
            ApiDisconnectReason? reason = null;

            consumer.Disconnected +=
                delegate(
                    object sender,
                    ApiDisconnectedEventArgs eventArgs
                )
                {
                    reason = eventArgs.Reason;
                };

            consumer.Start();
            AnnounceCompatibleProvider(bus);

            consumer.Stop();

            Assert.Equal(
                ApiDisconnectReason.ConsumerStopped,
                reason
            );

            Assert.False(consumer.IsConnected);
            Assert.False(consumer.IsStarted);
        }

        [Fact]
        public void FailingDisconnectedSubscriber_DoesNotRestoreConnection()
        {
            var bus = new InMemoryModMessageBus();

            using var consumer = CreateConsumer(bus);
            consumer.Disconnected +=
                delegate
                {
                    throw new InvalidOperationException(
                        "Subscriber failed."
                    );
                };

            consumer.Start();
            AnnounceCompatibleProvider(bus);

            consumer.Disconnect();

            Assert.False(consumer.IsConnected);

            Assert.IsType<InvalidOperationException>(
                consumer.LastError
            );
        }

        [Fact]
        public void FailingSubscriber_DoesNotPreventLaterSubscriber()
        {
            var bus = new InMemoryModMessageBus();

            using var consumer = CreateConsumer(bus);
            var laterSubscriberCalled = false;

            consumer.Disconnected +=
                delegate
                {
                    throw new InvalidOperationException(
                        "First subscriber failed."
                    );
                };

            consumer.Disconnected +=
                delegate
                {
                    laterSubscriberCalled = true;
                };

            consumer.Start();
            AnnounceCompatibleProvider(bus);

            consumer.Disconnect();

            Assert.True(laterSubscriberCalled);
        }

        private static void AnnounceCompatibleProvider(
            IModMessageBus bus
        )
        {
            bus.Send(
                ChannelId,
                ApiDiscoveryWireProtocol.CreateAnnouncement(
                    new ApiDescriptor(
                        "Mz.CommandAPI",
                        new SemanticVersion(1, 5, 0)
                    ),
                    Guid.NewGuid(),
                    Guid.Empty,
                    CreateEndpoints()
                )
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
            IModMessageBus bus
        )
        {
            return new ApiDiscoveryProvider(
                bus,
                ChannelId,
                new ApiDescriptor(
                    "Mz.CommandAPI",
                    new SemanticVersion(1, 5, 0)
                ),
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