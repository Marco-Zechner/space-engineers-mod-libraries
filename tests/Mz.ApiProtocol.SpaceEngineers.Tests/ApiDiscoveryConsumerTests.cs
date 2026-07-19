using System;
using System.Collections.Generic;
using Mz.ApiProtocol;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.SpaceEngineers.Tests
{
    public sealed class ApiDiscoveryConsumerTests
    {
        private const long ChannelId = 918273645L;

        [Fact]
        public void ProviderFirst_RequestDiscoveryConnectsConsumer()
        {
            var bus = new InMemoryModMessageBus();

            using (var provider = CreateProvider(
                bus,
                new SemanticVersion(1, 5, 0)
            ))
            using (var consumer = CreateConsumer(bus))
            {
                provider.Start();
                consumer.Start();

                Guid correlationId =
                    consumer.RequestDiscovery();

                Assert.NotEqual(Guid.Empty, correlationId);
                Assert.True(consumer.IsConnected);

                Assert.Equal(
                    "Mz.CommandAPI",
                    consumer.Connection.Descriptor.ApiId
                );

                Assert.Equal(
                    new SemanticVersion(1, 5, 0),
                    consumer.Connection.Descriptor.Version
                );
            }
        }

        [Fact]
        public void ConsumerFirst_LaterProviderAnnouncementConnectsConsumer()
        {
            var bus = new InMemoryModMessageBus();

            using (var consumer = CreateConsumer(bus))
            using (var provider = CreateProvider(
                bus,
                new SemanticVersion(1, 5, 0)
            ))
            {
                consumer.Start();
                consumer.RequestDiscovery();

                Assert.False(consumer.IsConnected);

                provider.Start();

                Assert.True(consumer.IsConnected);
                Assert.Equal(
                    Guid.Empty,
                    consumer.PendingCorrelationId
                );
            }
        }

        [Fact]
        public void IncompatibleProvider_IsObservedButNotConnected()
        {
            var bus = new InMemoryModMessageBus();

            using (var consumer = CreateConsumer(bus))
            using (var provider = CreateProvider(
                bus,
                new SemanticVersion(2, 0, 0)
            ))
            {
                consumer.Start();
                provider.Start();

                Assert.False(consumer.IsConnected);

                Assert.Equal(
                    ApiCompatibilityStatus.ProviderTooNew,
                    consumer.LastCompatibilityStatus
                );

                Assert.Equal(
                    new SemanticVersion(2, 0, 0),
                    consumer.LastObservedProvider.Version
                );
            }
        }

        [Fact]
        public void ResponseWithUnknownCorrelation_IsIgnored()
        {
            var bus = new InMemoryModMessageBus();

            using (var consumer = CreateConsumer(bus))
            {
                consumer.Start();

                bus.Send(
                    ChannelId,
                    ApiDiscoveryWireProtocol.CreateAnnouncement(
                        CreateDescriptor(
                            new SemanticVersion(1, 5, 0)
                        ),
                        Guid.NewGuid(),
                        CreateEndpoints()
                    )
                );

                Assert.False(consumer.IsConnected);
                Assert.Null(consumer.LastObservedProvider);
            }
        }

        [Fact]
        public void UnsolicitedAnnouncement_IsAcceptedWithoutRequest()
        {
            var bus = new InMemoryModMessageBus();

            using (var consumer = CreateConsumer(bus))
            {
                consumer.Start();

                bus.Send(
                    ChannelId,
                    ApiDiscoveryWireProtocol.CreateAnnouncement(
                        CreateDescriptor(
                            new SemanticVersion(1, 5, 0)
                        ),
                        Guid.Empty,
                        CreateEndpoints()
                    )
                );

                Assert.True(consumer.IsConnected);
            }
        }

        [Fact]
        public void DuplicateAnnouncements_DoNotReplaceConnection()
        {
            var bus = new InMemoryModMessageBus();

            using (var consumer = CreateConsumer(bus))
            {
                consumer.Start();

                object announcement =
                    ApiDiscoveryWireProtocol.CreateAnnouncement(
                        CreateDescriptor(
                            new SemanticVersion(1, 5, 0)
                        ),
                        Guid.Empty,
                        CreateEndpoints()
                    );

                bus.Send(ChannelId, announcement);

                ApiConnection firstConnection =
                    consumer.Connection;

                bus.Send(ChannelId, announcement);

                Assert.Same(
                    firstConnection,
                    consumer.Connection
                );
            }
        }

        [Fact]
        public void RequestDiscovery_BeforeStart_Throws()
        {
            using (var consumer = CreateConsumer(
                new InMemoryModMessageBus()
            ))
            {
                Assert.Throws<InvalidOperationException>(
                    delegate
                    {
                        consumer.RequestDiscovery();
                    }
                );
            }
        }

        [Fact]
        public void Stop_ClearsConnectionAndStopsListening()
        {
            var bus = new InMemoryModMessageBus();

            using (var consumer = CreateConsumer(bus))
            {
                consumer.Start();

                bus.Send(
                    ChannelId,
                    ApiDiscoveryWireProtocol.CreateAnnouncement(
                        CreateDescriptor(
                            new SemanticVersion(1, 5, 0)
                        ),
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
                        CreateDescriptor(
                            new SemanticVersion(1, 5, 0)
                        ),
                        Guid.Empty,
                        CreateEndpoints()
                    )
                );

                Assert.False(consumer.IsConnected);
            }
        }

        [Fact]
        public void MalformedAnnouncement_IsIgnoredWithoutError()
        {
            var bus = new InMemoryModMessageBus();

            using (var consumer = CreateConsumer(bus))
            {
                consumer.Start();
                bus.Send(ChannelId, "malformed");

                Assert.False(consumer.IsConnected);
                Assert.Null(consumer.LastError);
            }
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
                CreateDescriptor(version),
                CreateEndpoints()
            );
        }

        private static ApiDescriptor CreateDescriptor(
            SemanticVersion version
        )
        {
            return new ApiDescriptor(
                "Mz.CommandAPI",
                version
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