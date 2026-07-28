using System;
using System.Collections.Generic;
using Xunit;

namespace Mz.Networking.SpaceEngineers.Tests
{
    public sealed class SpaceEngineersNetworkClassificationTests
    {
        private const ushort ChannelId = 41000;
        private const string NetworkId = "Example.Network";

        [Fact]
        public void ExplicitIdentityPacket_DispatchesOnMatchingSession()
        {
            var gateway = new RecordingGateway(true, 100UL)
            {
                DeserializedEnvelope = CreateEnvelope()
            };

            byte[] packet =
                gateway.CreatePacket(
                    NetworkId,
                    new byte[] { 1, 2, 3 }
                );

            NetworkReceiveContext? observed = null;
            SpaceEngineersNetworkReceiveFailure? failure = null;

            using (
                var session =
                    new SpaceEngineersNetworkSession(
                        gateway,
                        ChannelId,
                        NetworkId,
                        value => failure = value
                    )
            )
            using (
                session.Endpoint.RegisterHandler(
                    "Command.Execute",
                    context => observed = context
                )
            )
            {
                Assert.True(session.UsesWireIdentity);
                Assert.Equal(NetworkId, session.NetworkId);
                Assert.True(session.Transport.UsesWireIdentity);
                Assert.Equal(NetworkId, session.Transport.NetworkId);
                gateway.DeliverRaw(packet, 200UL, false);
            }

            Assert.NotNull(observed);
            Assert.Null(failure);
            Assert.Equal(1, gateway.DeserializeCount);
        }

        [Fact]
        public void ForeignPacket_IsChannelConflict()
        {
            var gateway = new RecordingGateway(true, 100UL);
            SpaceEngineersNetworkReceiveFailure? observed = null;

            using (
                var session =
                    new SpaceEngineersNetworkSession(
                        gateway,
                        ChannelId,
                        NetworkId,
                        failure => observed = failure
                    )
            )
            {
                gateway.DeliverRaw(
                    new byte[] { 1, 2, 3, 4 },
                    200UL,
                    false
                );
            }

            Assert.NotNull(observed);
            Assert.Equal(
                SpaceEngineersNetworkReceiveFailureKind.ForeignPacket,
                observed!.Kind
            );
            Assert.True(observed.IsChannelConflict);
            Assert.Equal(NetworkId, observed.ExpectedNetworkId);
            Assert.Null(observed.ObservedNetworkId);
            Assert.Equal(0, gateway.DeserializeCount);
        }

        [Fact]
        public void MalformedMzWireHeader_IsNotChannelConflict()
        {
            var gateway = new RecordingGateway(true, 100UL);
            SpaceEngineersNetworkReceiveFailure? observed = null;

            using (
                var session =
                    new SpaceEngineersNetworkSession(
                        gateway,
                        ChannelId,
                        NetworkId,
                        failure => observed = failure
                    )
            )
            {
                gateway.DeliverRaw(
                    new byte[]
                    {
                        0x4D,
                        0x5A,
                        0x4E,
                        0x57,
                        1
                    },
                    200UL,
                    false
                );
            }

            Assert.NotNull(observed);
            Assert.Equal(
                SpaceEngineersNetworkReceiveFailureKind.MalformedWirePacket,
                observed!.Kind
            );
            Assert.False(observed.IsChannelConflict);
            Assert.Equal(NetworkId, observed.ExpectedNetworkId);
            Assert.Null(observed.ObservedNetworkId);
            Assert.Equal(0, gateway.DeserializeCount);
        }

        [Fact]
        public void OtherNetworkPacket_IsChannelConflict()
        {
            var gateway = new RecordingGateway(true, 100UL);

            byte[] packet =
                gateway.CreatePacket(
                    "Other.Network",
                    new byte[] { 4, 5, 6 }
                );

            SpaceEngineersNetworkReceiveFailure? observed = null;

            using (
                var session =
                    new SpaceEngineersNetworkSession(
                        gateway,
                        ChannelId,
                        NetworkId,
                        failure => observed = failure
                    )
            )
            {
                gateway.DeliverRaw(packet, 200UL, false);
            }

            Assert.NotNull(observed);
            Assert.Equal(
                SpaceEngineersNetworkReceiveFailureKind.NetworkMismatch,
                observed!.Kind
            );
            Assert.True(observed.IsChannelConflict);
            Assert.Equal("Other.Network", observed.ObservedNetworkId);
            Assert.Equal(0, gateway.DeserializeCount);
        }

        [Fact]
        public void OtherNetworkPacketWithUnsupportedVersion_IsNetworkMismatch()
        {
            var gateway = new RecordingGateway(true, 100UL);

            byte[] packet =
                gateway.CreatePacket(
                    "Other.Network",
                    new byte[] { 7 }
                );

            packet[4] = 2;

            SpaceEngineersNetworkReceiveFailure? observed = null;

            using (
                var session =
                    new SpaceEngineersNetworkSession(
                        gateway,
                        ChannelId,
                        NetworkId,
                        failure => observed = failure
                    )
            )
            {
                gateway.DeliverRaw(packet, 200UL, false);
            }

            Assert.NotNull(observed);
            Assert.Equal(
                SpaceEngineersNetworkReceiveFailureKind.NetworkMismatch,
                observed!.Kind
            );
            Assert.True(observed.IsChannelConflict);
            Assert.Equal("Other.Network", observed.ObservedNetworkId);
            Assert.Equal(0, gateway.DeserializeCount);
        }

        [Fact]
        public void UnsupportedWireVersion_IsNotChannelConflict()
        {
            var gateway = new RecordingGateway(true, 100UL);
            byte[] packet =
                gateway.CreatePacket(
                    NetworkId,
                    new byte[] { 7 }
                );

            packet[4] = 2;

            SpaceEngineersNetworkReceiveFailure? observed = null;

            using (
                var session =
                    new SpaceEngineersNetworkSession(
                        gateway,
                        ChannelId,
                        NetworkId,
                        failure => observed = failure
                    )
            )
            {
                gateway.DeliverRaw(packet, 200UL, false);
            }

            Assert.NotNull(observed);
            Assert.Equal(
                SpaceEngineersNetworkReceiveFailureKind.UnsupportedWireVersion,
                observed!.Kind
            );
            Assert.False(observed.IsChannelConflict);
            Assert.Equal(NetworkId, observed.ObservedNetworkId);
            Assert.Equal(0, gateway.DeserializeCount);
        }

        [Fact]
        public void MalformedOwnEnvelope_IsNotChannelConflict()
        {
            var gateway = new RecordingGateway(true, 100UL);
            byte[] packet =
                gateway.CreatePacket(
                    NetworkId,
                    new byte[] { 8, 9 }
                );

            var expected =
                new InvalidOperationException(
                    "Malformed own envelope."
                );

            gateway.DeserializeException = expected;
            SpaceEngineersNetworkReceiveFailure? observed = null;

            using (
                var session =
                    new SpaceEngineersNetworkSession(
                        gateway,
                        ChannelId,
                        NetworkId,
                        failure => observed = failure
                    )
            )
            {
                gateway.DeliverRaw(packet, 200UL, false);
            }

            Assert.NotNull(observed);
            Assert.Equal(
                SpaceEngineersNetworkReceiveFailureKind.MalformedOwnPacket,
                observed!.Kind
            );
            Assert.False(observed.IsChannelConflict);
            Assert.Same(expected, observed.Exception);
            Assert.Equal(NetworkId, observed.ObservedNetworkId);
        }

        [Fact]
        public void HandlerFailure_IsNotChannelConflict()
        {
            var gateway = new RecordingGateway(true, 100UL)
            {
                DeserializedEnvelope = CreateEnvelope()
            };

            byte[] packet =
                gateway.CreatePacket(
                    NetworkId,
                    new byte[] { 10 }
                );

            var expected =
                new InvalidOperationException(
                    "Application handler failed."
                );

            SpaceEngineersNetworkReceiveFailure? observed = null;

            using (
                var session =
                    new SpaceEngineersNetworkSession(
                        gateway,
                        ChannelId,
                        NetworkId,
                        failure => observed = failure
                    )
            )
            using (
                session.Endpoint.RegisterHandler(
                    "Command.Execute",
                    delegate
                    {
                        throw expected;
                    }
                )
            )
            {
                gateway.DeliverRaw(packet, 200UL, false);
            }

            Assert.NotNull(observed);
            Assert.Equal(
                SpaceEngineersNetworkReceiveFailureKind.HandlerFailure,
                observed!.Kind
            );
            Assert.False(observed.IsChannelConflict);
            Assert.Same(expected, observed.Exception);
        }

        private static NetworkEnvelope CreateEnvelope()
            => new NetworkEnvelope(
                "Command.Execute",
                200UL,
                false,
                new byte[] { 1 }
            );

        private sealed class RecordingGateway :
            ISpaceEngineersNetworkDeliveryGateway
        {
            public bool IsServer { get; }

            public ulong LocalPeerId { get; }

            public Action<ushort, byte[], ulong, bool>?
                RegisteredHandler { get; private set; }

            public byte[] SerializedBytes { get; set; } =
                new byte[] { 1 };

            public byte[]? ServerSendBytes { get; private set; }

            public NetworkEnvelope? DeserializedEnvelope { get; set; }

            public Exception? DeserializeException { get; set; }

            public int DeserializeCount { get; private set; }

            public RecordingGateway(bool isServer, ulong localPeerId)
            {
                IsServer = isServer;
                LocalPeerId = localPeerId;
            }

            public void RegisterSecureMessageHandler(
                ushort channelId,
                Action<ushort, byte[], ulong, bool> handler)
                => RegisteredHandler = handler;

            public void UnregisterSecureMessageHandler(
                ushort channelId,
                Action<ushort, byte[], ulong, bool> handler)
            {
            }

            public byte[] Serialize(NetworkEnvelope envelope)
                => SerializedBytes;

            public NetworkEnvelope Deserialize(byte[] serialized)
            {
                DeserializeCount++;

                if (DeserializeException != null)
                    throw DeserializeException;

                return DeserializedEnvelope!;
            }

            public bool SendToServer(
                ushort channelId,
                byte[] serialized)
                => SendToServer(
                    channelId,
                    serialized,
                    true
                );

            public bool SendToServer(
                ushort channelId,
                byte[] serialized,
                bool reliable)
            {
                ServerSendBytes = serialized;
                return true;
            }

            public bool SendToPeer(
                ushort channelId,
                byte[] serialized,
                ulong peerId)
                => SendToPeer(
                    channelId,
                    serialized,
                    peerId,
                    true
                );

            public bool SendToPeer(
                ushort channelId,
                byte[] serialized,
                ulong peerId,
                bool reliable)
                => true;

            public void GetPlayerIds(List<ulong> playerIds)
            {
            }

            public byte[] CreatePacket(
                string networkId,
                byte[] serializedEnvelope)
            {
                SerializedBytes = serializedEnvelope;

                var transport =
                    new SpaceEngineersNetworkTransport(
                        this,
                        ChannelId,
                        networkId
                    );

                transport.SendToServer(CreateEnvelope());

                return ServerSendBytes!;
            }

            public void DeliverRaw(
                byte[] packet,
                ulong senderPeerId,
                bool senderIsServer)
            {
                RegisteredHandler!(
                    ChannelId,
                    packet,
                    senderPeerId,
                    senderIsServer
                );
            }
        }
    }
}
