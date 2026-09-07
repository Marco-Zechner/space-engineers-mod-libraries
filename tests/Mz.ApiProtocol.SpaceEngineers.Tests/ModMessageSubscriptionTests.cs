using System;
using System.Collections.Generic;
using Xunit;

namespace Mz.ApiProtocol.SpaceEngineers.Tests
{
    public sealed class ModMessageSubscriptionTests
    {
        [Fact]
        public void Constructor_RegistersHandlerOnExpectedChannel()
        {
            var bus = new RecordingModMessageBus();

            Action<object> handler = delegate { };

            using var subscription = new ModMessageSubscription(bus, 123456789L, handler);
            Assert.Equal(123456789L, subscription.ChannelId);

            Registration registration = Assert.Single(bus.Registrations);

            Assert.Equal(123456789L, registration.ChannelId);
            Assert.Same(handler, registration.Handler);
        }

        [Fact]
        public void Constructor_NullBus_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                { new ModMessageSubscription(null!, 123L, delegate { }); }
            );
        }

        [Fact]
        public void Constructor_NullHandler_ThrowsArgumentNullException()
        {
            var bus = new RecordingModMessageBus();

            Assert.Throws<ArgumentNullException>(() =>
                { new ModMessageSubscription(bus, 123L, null!); }
            );

            Assert.Empty(bus.Registrations);
        }

        [Fact]
        public void Dispose_UnregistersExactRegisteredHandler()
        {
            var bus = new RecordingModMessageBus();

            Action<object> handler = delegate { };

            var subscription = new ModMessageSubscription(bus, 123456789L, handler);

            subscription.Dispose();

            Registration registration = Assert.Single(bus.Unregistrations);

            Assert.Equal(123456789L, registration.ChannelId);
            Assert.Same(handler, registration.Handler);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_UnregistersOnce()
        {
            var bus = new RecordingModMessageBus();

            var subscription = new ModMessageSubscription(bus, 123L, delegate { });

            subscription.Dispose();
            subscription.Dispose();

            Assert.Single(bus.Unregistrations);
        }

        [Fact]
        public void Dispose_UnregisterFailureCanBeRetried()
        {
            var bus = new RecordingModMessageBus
            {
                RemainingUnregisterFailures = 1
            };

            var subscription = new ModMessageSubscription(bus, 123L, delegate { });

            Assert.Throws<InvalidOperationException>(subscription.Dispose);

            subscription.Dispose();

            Assert.Equal(2, bus.UnregisterAttemptCount);
            Assert.Single(bus.Unregistrations);
        }

        [Fact]
        public void Constructor_RegisterFailureDoesNotCreateRegistration()
        {
            var bus = new RecordingModMessageBus
            {
                RegisterException = new InvalidOperationException("Registration failed.")
            };

            Assert.Throws<InvalidOperationException>(() =>
                { new ModMessageSubscription(bus, 123L, delegate { }); }
            );

            Assert.Empty(bus.Registrations);
            Assert.Empty(bus.Unregistrations);
        }

        private sealed class RecordingModMessageBus :
            IModMessageBus
        {
            public List<Registration> Registrations { get; } = [];

            public List<Registration> Unregistrations { get; } = [];

            public Exception? RegisterException { get; set; }

            public int RemainingUnregisterFailures { get; set; }

            public int UnregisterAttemptCount { get; private set; }

            public void RegisterHandler(long channelId, Action<object> handler)
            {
                if (RegisterException != null)
                    throw RegisterException;

                Registrations.Add(new(channelId, handler));
            }

            public void UnregisterHandler(long channelId, Action<object> handler)
            {
                UnregisterAttemptCount++;

                if (RemainingUnregisterFailures > 0)
                {
                    RemainingUnregisterFailures--;

                    throw new InvalidOperationException("Unregistration failed.");
                }

                Unregistrations.Add(new(channelId, handler));

            }

            public void Send(long channelId, object payload) { }
        }

        private sealed class Registration(long channelId, Action<object> handler)
        {
            public long ChannelId { get; } = channelId;

            public Action<object> Handler { get; } = handler;
        }
    }
}