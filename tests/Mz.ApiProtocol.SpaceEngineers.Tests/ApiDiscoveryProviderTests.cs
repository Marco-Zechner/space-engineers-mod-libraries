using System;
using System.Collections.Generic;
using Mz.ApiProtocol;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.SpaceEngineers.Tests
{
    public sealed class ApiDiscoveryProviderTests
    {
        private const long ChannelId = 918273645L;

        [Fact]
        public void Start_RegistersAndBroadcastsUnsolicitedAnnouncement()
        {
            var bus = new InMemoryModMessageBus();
            var provider = CreateProvider(bus);

            provider.Start();

            Assert.True(provider.IsStarted);
            Assert.Equal(1, bus.RegistrationCount);

            ApiAnnouncement announcement =
                ParseLastAnnouncement(bus);

            Assert.Equal(
                Guid.Empty,
                announcement.CorrelationId
            );

            provider.Dispose();
        }

        [Fact]
        public void MatchingRequest_ProducesCorrelatedAnnouncement()
        {
            var bus = new InMemoryModMessageBus();
            var provider = CreateProvider(bus);

            provider.Start();

            Guid correlationId = Guid.NewGuid();

            bus.Send(
                ChannelId,
                ApiDiscoveryWireProtocol.CreateRequest(
                    "Mz.CommandAPI",
                    correlationId
                )
            );

            ApiAnnouncement announcement =
                ParseLastAnnouncement(bus);

            Assert.Equal(
                correlationId,
                announcement.CorrelationId
            );

            provider.Dispose();
        }

        [Fact]
        public void DifferentApiRequest_IsIgnored()
        {
            var bus = new InMemoryModMessageBus();
            var provider = CreateProvider(bus);

            provider.Start();

            int sendCountBeforeRequest = bus.SendCount;

            bus.Send(
                ChannelId,
                ApiDiscoveryWireProtocol.CreateRequest(
                    "Mz.OtherAPI",
                    Guid.NewGuid()
                )
            );

            Assert.Equal(
                sendCountBeforeRequest + 1,
                bus.SendCount
            );

            provider.Dispose();
        }

        [Fact]
        public void MalformedMessage_IsIgnoredWithoutError()
        {
            var bus = new InMemoryModMessageBus();
            var provider = CreateProvider(bus);

            provider.Start();
            bus.Send(ChannelId, "malformed");

            Assert.Null(provider.LastError);

            provider.Dispose();
        }

        [Fact]
        public void StartAndStop_AreIdempotent()
        {
            var bus = new InMemoryModMessageBus();
            var provider = CreateProvider(bus);

            provider.Start();
            provider.Start();

            Assert.Equal(1, bus.RegistrationCount);

            provider.Stop();
            provider.Stop();

            Assert.False(provider.IsStarted);
            Assert.Equal(1, bus.UnregistrationCount);
        }

        [Fact]
        public void Announce_BeforeStart_ThrowsInvalidOperationException()
        {
            var provider = CreateProvider(
                new InMemoryModMessageBus()
            );

            Assert.Throws<InvalidOperationException>(
                delegate
                {
                    provider.Announce();
                }
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
                new Dictionary<string, Delegate>
                {
                    {
                        "Ping",
                        (Action)delegate
                        {
                        }
                    }
                }
            );
        }

        private static ApiAnnouncement ParseLastAnnouncement(
            InMemoryModMessageBus bus
        )
        {
            object payload =
                bus.SentPayloads[
                    bus.SentPayloads.Count - 1
                ];

            bool success =
                ApiDiscoveryWireProtocol.TryParseAnnouncement(
                    payload,
                    out ApiAnnouncement announcement
                );

            Assert.True(success);
            return announcement;
        }
    }
}