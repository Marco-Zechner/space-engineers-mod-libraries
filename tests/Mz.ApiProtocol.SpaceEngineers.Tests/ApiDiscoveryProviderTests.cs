using System;
using System.Collections.Generic;
using Xunit;

namespace Mz.ApiProtocol.SpaceEngineers.Tests
{
    public sealed class ApiDiscoveryProviderTests
    {
        private const long ChannelId = ApiProtocolChannels.Discovery;

        [Fact]
        public void Start_RegistersAndBroadcastsUnsolicitedAnnouncement()
        {
            var bus = new InMemoryModMessageBus();
            ApiDiscoveryProvider provider = CreateProvider(bus);

            provider.Start();

            Assert.True(provider.IsStarted);
            Assert.Equal(1, bus.RegistrationCount);

            ApiAnnouncement announcement = ParseLastAnnouncement(bus);

            Assert.Equal(Guid.Empty, announcement.CorrelationId);

            provider.Dispose();
        }

        [Fact]
        public void MatchingRequest_ProducesCorrelatedAnnouncement()
        {
            var bus = new InMemoryModMessageBus();
            ApiDiscoveryProvider provider = CreateProvider(bus);

            provider.Start();

            var correlationId = Guid.NewGuid();

            bus.Send(
                ChannelId,
                ApiDiscoveryWireProtocol.CreateRequest(
                    CreateDependency(),
                    correlationId
                )
            );

            ApiAnnouncement announcement = ParseLastAnnouncement(bus);

            Assert.Equal(correlationId, announcement.CorrelationId);

            provider.Dispose();
        }

        [Fact]
        public void DifferentApiRequest_IsIgnored()
        {
            var bus = new InMemoryModMessageBus();
            ApiDiscoveryProvider provider = CreateProvider(bus);

            provider.Start();

            int sendCountBeforeRequest = bus.SendCount;

            bus.Send(
                ChannelId,
                ApiDiscoveryWireProtocol.CreateRequest(
                    CreateDependency("Mz.OtherAPI"),
                    Guid.NewGuid()
                )
            );

            Assert.Equal(sendCountBeforeRequest + 1, bus.SendCount);
            
            provider.Dispose();
        }

        [Fact]
        public void MalformedMessage_IsIgnoredWithoutError()
        {
            var bus = new InMemoryModMessageBus();
            ApiDiscoveryProvider provider = CreateProvider(bus);

            provider.Start();
            bus.Send(ChannelId, "malformed");

            Assert.Null(provider.LastError);

            provider.Dispose();
        }

        [Fact]
        public void StartAndStop_AreIdempotent()
        {
            var bus = new InMemoryModMessageBus();
            ApiDiscoveryProvider provider = CreateProvider(bus);

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
            ApiDiscoveryProvider provider = CreateProvider(new InMemoryModMessageBus());

            Assert.Throws<InvalidOperationException>(provider.Announce);
        }

        private static ApiDiscoveryProvider CreateProvider(IModMessageBus bus) 
            => new(
            bus,
            CreateProviderIdentity(),
            new ApiDescriptor("Mz.CommandAPI", new(1, 5, 0)),
            new Dictionary<string, Delegate> { { "Ping", (Action)delegate { } } }
        );

        private static ApiAnnouncement ParseLastAnnouncement(InMemoryModMessageBus bus)
        {
            object? payload = bus.SentPayloads[bus.SentPayloads.Count - 1];

            bool success = ApiDiscoveryWireProtocol.TryParseAnnouncement(payload, out ApiAnnouncement announcement);

            Assert.True(success);
            return announcement;
        }
        
        private static ApiModIdentity CreateProviderIdentity() 
            => new("Mz.CommandApiMod", "Command API", new(1, 4, 0));

        private static ApiDependencyDescriptor CreateDependency(string apiId = "Mz.CommandAPI") 
            => new(
                new ApiModIdentity("Mz.ConsumerMod", "Consumer Mod", new(2, 0, 0)),
                new ApiRequirement(apiId, new ApiVersionRange(new(1, 0, 0), new(2, 0, 0))),
                ApiDependencyKind.Optional,
                "Adds Command API integration"
            );
    }
}