using System;
using System.Collections.Generic;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.SpaceEngineers.Tests
{
    public sealed class ApiDiscoveryProviderConsumerObservationTests
    {
        private const long ChannelId = 918273645L;

        [Fact]
        public void CompatibleRequest_RaisesObservationAndResponds()
        {
            var bus = new InMemoryModMessageBus();

            using var provider = CreateProvider(bus);

            ApiConsumerObservedEventArgs observed = null!;

            provider.ConsumerObserved +=
                delegate(ApiConsumerObservedEventArgs eventArgs)
                {
                    observed = eventArgs;
                };

            provider.Start();

            int announcementsBeforeRequest =
                CountAnnouncements(bus);

            Guid correlationId = Guid.NewGuid();

            bus.Send(
                ChannelId,
                ApiDiscoveryWireProtocol.CreateRequest(
                    CreateDependency(
                        new SemanticVersion(1, 0, 0),
                        new SemanticVersion(2, 0, 0),
                        ApiDependencyKind.Optional
                    ),
                    correlationId
                )
            );

            Assert.NotNull(observed);

            Assert.Equal(
                "Mz.ConsumerMod",
                observed.Consumer.Id
            );

            Assert.Equal(
                ApiDependencyKind.Optional,
                observed.Dependency.Kind
            );

            Assert.Equal(
                ApiCompatibilityStatus.Compatible,
                observed.CompatibilityStatus
            );

            Assert.Equal(
                correlationId,
                observed.CorrelationId
            );

            Assert.Equal(
                announcementsBeforeRequest + 1,
                CountAnnouncements(bus)
            );
        }

        [Fact]
        public void ProviderTooOld_RaisesObservationWithoutResponding()
        {
            var bus = new InMemoryModMessageBus();

            using var provider = CreateProvider(bus);

            ApiConsumerObservedEventArgs observed = null!;

            provider.ConsumerObserved +=
                delegate(ApiConsumerObservedEventArgs eventArgs)
                {
                    observed = eventArgs;
                };

            provider.Start();

            int announcementsBeforeRequest =
                CountAnnouncements(bus);

            bus.Send(
                ChannelId,
                ApiDiscoveryWireProtocol.CreateRequest(
                    CreateDependency(
                        new SemanticVersion(2, 0, 0),
                        new SemanticVersion(3, 0, 0),
                        ApiDependencyKind.Required
                    ),
                    Guid.NewGuid()
                )
            );

            Assert.NotNull(observed);

            Assert.Equal(
                ApiCompatibilityStatus.ProviderTooOld,
                observed.CompatibilityStatus
            );

            Assert.Equal(
                ApiDependencyKind.Required,
                observed.Dependency.Kind
            );

            Assert.Equal(
                announcementsBeforeRequest,
                CountAnnouncements(bus)
            );
        }

        [Fact]
        public void ProviderTooNew_RaisesObservationWithoutResponding()
        {
            var bus = new InMemoryModMessageBus();

            using var provider = CreateProvider(bus);

            ApiConsumerObservedEventArgs observed = null!;

            provider.ConsumerObserved +=
                delegate(ApiConsumerObservedEventArgs eventArgs)
                {
                    observed = eventArgs;
                };

            provider.Start();

            int announcementsBeforeRequest =
                CountAnnouncements(bus);

            bus.Send(
                ChannelId,
                ApiDiscoveryWireProtocol.CreateRequest(
                    CreateDependency(
                        new SemanticVersion(1, 0, 0),
                        new SemanticVersion(1, 5, 0),
                        ApiDependencyKind.Optional
                    ),
                    Guid.NewGuid()
                )
            );

            Assert.NotNull(observed);

            Assert.Equal(
                ApiCompatibilityStatus.ProviderTooNew,
                observed.CompatibilityStatus
            );

            Assert.Equal(
                announcementsBeforeRequest,
                CountAnnouncements(bus)
            );
        }

        [Fact]
        public void FailingObservationSubscriber_DoesNotPreventResponse()
        {
            var bus = new InMemoryModMessageBus();

            using var provider = CreateProvider(bus);

            provider.ConsumerObserved +=
                delegate
                {
                    throw new InvalidOperationException(
                        "Subscriber failed."
                    );
                };

            provider.Start();

            int announcementsBeforeRequest =
                CountAnnouncements(bus);

            bus.Send(
                ChannelId,
                ApiDiscoveryWireProtocol.CreateRequest(
                    CreateDependency(
                        new SemanticVersion(1, 0, 0),
                        new SemanticVersion(2, 0, 0),
                        ApiDependencyKind.Optional
                    ),
                    Guid.NewGuid()
                )
            );

            Assert.Equal(
                announcementsBeforeRequest + 1,
                CountAnnouncements(bus)
            );

            Assert.IsType<InvalidOperationException>(
                provider.LastError
            );
        }

        [Fact]
        public void FailingObservationSubscriber_DoesNotPreventLaterSubscriber()
        {
            var bus = new InMemoryModMessageBus();

            using var provider = CreateProvider(bus);

            bool laterSubscriberCalled = false;

            provider.ConsumerObserved +=
                delegate
                {
                    throw new InvalidOperationException(
                        "First subscriber failed."
                    );
                };

            provider.ConsumerObserved +=
                delegate
                {
                    laterSubscriberCalled = true;
                };

            provider.Start();

            bus.Send(
                ChannelId,
                ApiDiscoveryWireProtocol.CreateRequest(
                    CreateDependency(
                        new SemanticVersion(1, 0, 0),
                        new SemanticVersion(2, 0, 0),
                        ApiDependencyKind.Optional
                    ),
                    Guid.NewGuid()
                )
            );

            Assert.True(laterSubscriberCalled);
        }

        private static ApiDiscoveryProvider CreateProvider(
            IModMessageBus bus
        )
        {
            return new ApiDiscoveryProvider(
                bus,
                ChannelId,
                new ApiModIdentity(
                    "Mz.CommandApiMod",
                    "Command API",
                    new SemanticVersion(1, 4, 0)
                ),
                new ApiDescriptor(
                    "Mz.CommandAPI",
                    new SemanticVersion(1, 5, 0)
                ),
                new Dictionary<string, Delegate>()
            );
        }

        private static ApiDependencyDescriptor CreateDependency(
            SemanticVersion minimum,
            SemanticVersion maximum,
            ApiDependencyKind kind
        )
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
                        minimum,
                        maximum
                    )
                ),
                kind,
                "Adds command integration"
            );
        }

        private static int CountAnnouncements(
            InMemoryModMessageBus bus
        )
        {
            int count = 0;

            for (
                int index = 0;
                index < bus.SentPayloads.Count;
                index++
            )
            {
                if (ApiDiscoveryWireProtocol.TryParseAnnouncement(
                    bus.SentPayloads[index],
                    out ApiAnnouncement announcement
                ))
                {
                    count++;
                }
            }

            return count;
        }
    }
}