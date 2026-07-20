using System;
using Xunit;

namespace Mz.ApiProtocol.SpaceEngineers.Tests
{
    public sealed class ModMessageBusTestDoubleTests
    {
        [Fact]
        public void Send_PreservesChannelAndPayload()
        {
            var bus = new RecordingModMessageBus();
            var payload = new object();

            bus.Send(long.MinValue, payload);

            Assert.Equal(long.MinValue, bus.LastChannelId);
            Assert.Same(payload, bus.LastPayload);
        }

        [Fact]
        public void Send_AllowsNullPayload()
        {
            var bus = new RecordingModMessageBus();

            bus.Send(0L, null!);

            Assert.Equal(0L, bus.LastChannelId);
            Assert.Null(bus.LastPayload);
        }

        private sealed class RecordingModMessageBus :
            IModMessageBus
        {
            public long LastChannelId { get; private set; }

            public object? LastPayload { get; private set; }

            public void RegisterHandler(
                long channelId,
                Action<object> handler
            )
            {
            }

            public void UnregisterHandler(
                long channelId,
                Action<object> handler
            )
            {
            }

            public void Send(
                long channelId,
                object payload
            )
            {
                LastChannelId = channelId;
                LastPayload = payload;
            }
        }
    }
}