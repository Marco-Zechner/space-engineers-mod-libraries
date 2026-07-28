using System;
using System.Collections.Generic;
using Xunit;

namespace Mz.Networking.SpaceEngineers.Tests
{
    public sealed class SpaceEngineersNetworkSessionTests
    {
        [Fact]
        public void ConstructorAndDispose_OwnExactSecureHandlerRegistration()
        {
            var gateway = new RecordingGateway(
                true,
                100UL
            );

            var session = new SpaceEngineersNetworkSession(
                gateway,
                41000,
                delegate
                {
                }
            );

            Assert.Equal((ushort)41000, gateway.RegisteredChannel);
            Assert.NotNull(gateway.RegisteredHandler);
            Assert.False(session.UsesWireIdentity);
            Assert.Null(session.NetworkId);

            Action<ushort, byte[], ulong, bool> registered =
                gateway.RegisteredHandler!;

            session.Dispose();
            session.Dispose();

            Assert.Equal((ushort)41000, gateway.UnregisteredChannel);
            Assert.Same(registered, gateway.UnregisteredHandler);
            Assert.Equal(1, gateway.UnregisterCount);
        }

        [Fact]
        public void ReceivedPacket_DispatchesUsingTrustedTransportMetadata()
        {
            var gateway = new RecordingGateway(
                true,
                100UL
            );

            gateway.DeserializedEnvelope =
                new NetworkEnvelope(
                    "Command.Execute",
                    999UL,
                    true,
                    new byte[] { 1 }
                );

            NetworkReceiveContext? observed = null;

            using (
                var session = new SpaceEngineersNetworkSession(
                    gateway,
                    41000,
                    delegate
                    {
                    }
                )
            )
            using (
                session.Endpoint.RegisterHandler(
                    "Command.Execute",
                    delegate(NetworkReceiveContext context)
                    {
                        observed = context;
                    }
                )
            )
            {
                gateway.Deliver(
                    new byte[] { 9, 8, 7 },
                    200UL,
                    false
                );
            }

            Assert.NotNull(observed);
            Assert.Equal(200UL, observed!.Envelope.OriginalSenderId);
            Assert.False(observed.Envelope.IsRelay);
            Assert.True(observed.OriginalSenderWasCorrected);
            Assert.True(observed.RelayFlagWasCorrected);
        }

        [Fact]
        public void ReceivedPacket_DeserializeFailure_ReportsFailure()
        {
            var gateway = new RecordingGateway(
                true,
                100UL
            );

            var expected =
                new InvalidOperationException(
                    "Malformed packet."
                );

            gateway.DeserializeException = expected;

            SpaceEngineersNetworkReceiveFailure? observed = null;

            using (
                var session = new SpaceEngineersNetworkSession(
                    gateway,
                    41000,
                    delegate(
                        SpaceEngineersNetworkReceiveFailure failure
                    )
                    {
                        observed = failure;
                    }
                )
            )
            {
                gateway.Deliver(
                    new byte[] { 4, 5, 6 },
                    300UL,
                    false
                );
            }

            Assert.NotNull(observed);
            Assert.Equal((ushort)41000, observed!.ChannelId);
            Assert.Equal(300UL, observed.SenderPeerId);
            Assert.False(observed.SenderIsServer);
            Assert.Same(expected, observed.Exception);
            Assert.Equal(
                new byte[] { 4, 5, 6 },
                observed.SerializedMessage
            );
            Assert.Equal(
                SpaceEngineersNetworkReceiveFailureKind.MalformedOwnPacket,
                observed.Kind
            );
            Assert.False(observed.IsChannelConflict);
            Assert.Null(observed.ExpectedNetworkId);
            Assert.Null(observed.ObservedNetworkId);
        }

        [Fact]
        public void Transport_SendToOthers_ExcludesLocalAndRequestedPeer()
        {
            var gateway = new RecordingGateway(
                true,
                100UL
            );

            gateway.PlayerIds.Add(100UL);
            gateway.PlayerIds.Add(200UL);
            gateway.PlayerIds.Add(300UL);

            var transport =
                new SpaceEngineersNetworkTransport(
                    gateway,
                    41000
                );

            transport.SendToOthers(
                CreateEnvelope(),
                200UL
            );

            PeerSend sent =
                Assert.Single(gateway.PeerSends);

            Assert.Equal((ushort)41000, sent.ChannelId);
            Assert.Equal(300UL, sent.PeerId);
            Assert.Equal(1, gateway.SerializeCount);
        }

        [Fact]
        public void Transport_SendToEveryone_SkipsLocalServerPeer()
        {
            var gateway = new RecordingGateway(
                true,
                100UL
            );

            gateway.PlayerIds.Add(100UL);
            gateway.PlayerIds.Add(200UL);
            gateway.PlayerIds.Add(300UL);

            var transport =
                new SpaceEngineersNetworkTransport(
                    gateway,
                    41000
                );

            transport.SendToEveryone(CreateEnvelope());

            Assert.Equal(2, gateway.PeerSends.Count);
            Assert.Equal(200UL, gateway.PeerSends[0].PeerId);
            Assert.Equal(300UL, gateway.PeerSends[1].PeerId);
            Assert.Equal(1, gateway.SerializeCount);
        }

        [Fact]
        public void Transport_SendToServer_SerializesOnceOnConfiguredChannel()
        {
            var gateway = new RecordingGateway(
                false,
                200UL
            );

            var transport =
                new SpaceEngineersNetworkTransport(
                    gateway,
                    41000
                );

            transport.SendToServer(CreateEnvelope());

            Assert.Equal(1, gateway.SerializeCount);
            Assert.Equal((ushort)41000, gateway.ServerSendChannel);

            Assert.Equal(
                gateway.SerializedBytes,
                gateway.ServerSendBytes
            );
            Assert.False(transport.UsesWireIdentity);
            Assert.Null(transport.NetworkId);
            Assert.True(gateway.ServerSendReliable);
        }
        [Fact]
        public void Transport_SendToServer_UnreliablePassesFalse()
        {
            var gateway = new RecordingGateway(false, 200UL);
            var transport = new SpaceEngineersNetworkTransport(gateway, 41000);

            transport.SendToServer(CreateEnvelope(), NetworkDeliveryMode.Unreliable);

            Assert.False(gateway.ServerSendReliable);
        }

        [Fact]
        public void Transport_SendToPeer_UnreliablePassesFalse()
        {
            var gateway = new RecordingGateway(true, 100UL);
            var transport = new SpaceEngineersNetworkTransport(gateway, 41000);

            transport.SendToPeer(CreateEnvelope(), 200UL, NetworkDeliveryMode.Unreliable);

            Assert.False(Assert.Single(gateway.PeerSends).Reliable);
        }

        [Fact]
        public void Transport_SendToServer_UnreliableOverLimitThrowsBeforeSend()
        {
            var gateway = new RecordingGateway(false, 200UL)
            {
                SerializedBytes = new byte[1025]
            };

            var transport = new SpaceEngineersNetworkTransport(gateway, 41000);

            Assert.Throws<InvalidOperationException>(
                delegate
                {
                    transport.SendToServer(CreateEnvelope(), NetworkDeliveryMode.Unreliable);
                }
            );

            Assert.Equal(0, gateway.ServerSendCount);
        }

        [Fact]
        public void Transport_SendToServer_UnreliableAtLimitSucceeds()
        {
            var gateway = new RecordingGateway(false, 200UL)
            {
                SerializedBytes = new byte[1024]
            };

            var transport = new SpaceEngineersNetworkTransport(gateway, 41000);

            transport.SendToServer(CreateEnvelope(), NetworkDeliveryMode.Unreliable);

            Assert.Equal(1, gateway.ServerSendCount);
        }


        private static NetworkEnvelope CreateEnvelope()
        {
            return new NetworkEnvelope(
                "Command.Execute",
                200UL,
                false,
                new byte[] { 1, 2, 3 }
            );
        }

        private sealed class RecordingGateway :
            ISpaceEngineersNetworkDeliveryGateway
        {
            public bool IsServer { get; }

            public ulong LocalPeerId { get; }

            public List<ulong> PlayerIds { get; } =
                new List<ulong>();

            public List<PeerSend> PeerSends { get; } =
                new List<PeerSend>();

            public Action<ushort, byte[], ulong, bool>?
                RegisteredHandler { get; private set; }

            public Action<ushort, byte[], ulong, bool>?
                UnregisteredHandler { get; private set; }

            public ushort RegisteredChannel { get; private set; }

            public ushort UnregisteredChannel { get; private set; }

            public int UnregisterCount { get; private set; }

            public int SerializeCount { get; private set; }

            public byte[] SerializedBytes { get; set; } =
                new byte[] { 7, 7, 7 };

            public ushort ServerSendChannel { get; private set; }

            public byte[]? ServerSendBytes { get; private set; }

            public int ServerSendCount { get; private set; }

            public bool ServerSendReliable { get; private set; }

            public NetworkEnvelope? DeserializedEnvelope { get; set; }

            public Exception? DeserializeException { get; set; }

            public RecordingGateway(bool isServer, ulong localPeerId)
            {
                IsServer = isServer;
                LocalPeerId = localPeerId;
            }

            public void RegisterSecureMessageHandler(ushort channelId, Action<ushort, byte[], ulong, bool> handler)
            {
                RegisteredChannel = channelId;
                RegisteredHandler = handler;
            }

            public void UnregisterSecureMessageHandler(ushort channelId, Action<ushort, byte[], ulong, bool> handler)
            {
                UnregisterCount++;
                UnregisteredChannel = channelId;
                UnregisteredHandler = handler;
            }

            public byte[] Serialize(NetworkEnvelope envelope)
            {
                SerializeCount++;
                return SerializedBytes;
            }

            public NetworkEnvelope Deserialize(byte[] serialized)
            {
                if (DeserializeException != null)
                    throw DeserializeException;

                return DeserializedEnvelope!;
            }

            public bool SendToServer(ushort channelId, byte[] serialized)
                => SendToServer(channelId, serialized, true);

            public bool SendToServer(ushort channelId, byte[] serialized, bool reliable)
            {
                ServerSendCount++;
                ServerSendChannel = channelId;
                ServerSendBytes = serialized;
                ServerSendReliable = reliable;
                return true;
            }

            public bool SendToPeer(ushort channelId, byte[] serialized, ulong peerId)
                => SendToPeer(channelId, serialized, peerId, true);

            public bool SendToPeer(ushort channelId, byte[] serialized, ulong peerId, bool reliable)
            {
                PeerSends.Add(new PeerSend(channelId, serialized, peerId, reliable));
                return true;
            }

            public void GetPlayerIds(List<ulong> playerIds)
                => playerIds.AddRange(PlayerIds);

            public void Deliver(byte[] serialized, ulong senderPeerId, bool senderIsServer)
            {
                RegisteredHandler!(
                    RegisteredChannel,
                    serialized,
                    senderPeerId,
                    senderIsServer
                );
            }
        }

        private sealed class PeerSend
        {
            public ushort ChannelId { get; }

            public byte[] Serialized { get; }

            public ulong PeerId { get; }

            public bool Reliable { get; }

            public PeerSend(ushort channelId, byte[] serialized, ulong peerId, bool reliable)
            {
                ChannelId = channelId;
                Serialized = serialized;
                PeerId = peerId;
                Reliable = reliable;
            }
        }
    }
}
