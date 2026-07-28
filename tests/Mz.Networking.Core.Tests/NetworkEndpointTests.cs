using System;
using System.Collections.Generic;
using Xunit;

namespace Mz.Networking.Tests
{
    public sealed class NetworkEndpointTests
    {
        [Fact]
        public void SendToServer_Client_SendsEnvelopeThroughTransport()
        {
            var transport = new RecordingTransport(
                false,
                101UL
            );

            var endpoint = new NetworkEndpoint(transport);

            endpoint.SendToServer(
                "Command.Execute",
                new byte[] { 1, 2, 3 }
            );

            NetworkEnvelope envelope =
                Assert.Single(transport.ServerMessages);

            Assert.Equal(
                "Command.Execute",
                envelope.MessageType
            );

            Assert.Equal(
                101UL,
                envelope.OriginalSenderId
            );

            Assert.False(envelope.IsRelay);

            Assert.Equal(
                new byte[] { 1, 2, 3 },
                envelope.Payload
            );

            Assert.Equal(
                NetworkDeliveryMode.Reliable,
                Assert.Single(transport.ServerDeliveryModes)
            );
        }
        [Fact]
        public void SendToServer_ClientUnreliable_UsesSelectedDelivery()
        {
            var transport = new RecordingTransport(false, 101UL);
            var endpoint = new NetworkEndpoint(transport);

            endpoint.SendToServer("Physics.State", new byte[] { 1 }, NetworkDeliveryMode.Unreliable);

            Assert.Equal(
                NetworkDeliveryMode.Unreliable,
                Assert.Single(transport.ServerDeliveryModes)
            );
        }

        [Fact]
        public void SendToServer_ClientUnreliable_LegacyTransportThrows()
        {
            var endpoint = new NetworkEndpoint(new LegacyRecordingTransport(false, 101UL));

            Assert.Throws<NotSupportedException>(
                delegate
                {
                    endpoint.SendToServer("Physics.State", new byte[0], NetworkDeliveryMode.Unreliable);
                }
            );
        }

        [Fact]
        public void SendToPlayer_ServerUnreliable_UsesSelectedDelivery()
        {
            var transport = new RecordingTransport(true, 500UL);
            var endpoint = new NetworkEndpoint(transport);

            endpoint.SendToPlayer("Physics.State", new byte[] { 1 }, 200UL, NetworkDeliveryMode.Unreliable);

            Assert.Equal(
                NetworkDeliveryMode.Unreliable,
                Assert.Single(transport.PeerMessages).DeliveryMode
            );
        }


        [Fact]
        public void SendToServer_Server_DispatchesLocally()
        {
            var transport = new RecordingTransport(
                true,
                500UL
            );

            var endpoint = new NetworkEndpoint(transport);
            NetworkReceiveContext? observed = null;

            using (
                endpoint.RegisterHandler(
                    "Command.Execute",
                    delegate(NetworkReceiveContext context)
                    {
                        observed = context;
                    }
                )
            )
            {
                endpoint.SendToServer(
                    "Command.Execute",
                    new byte[] { 9 }
                );
            }

            Assert.NotNull(observed);

            Assert.Equal(
                500UL,
                observed!.Envelope.OriginalSenderId
            );

            Assert.Empty(transport.ServerMessages);
        }

        [Fact]
        public void Receive_ServerRelayToOthers_UsesValidatedSender()
        {
            var transport = new RecordingTransport(
                true,
                500UL
            );

            var endpoint = new NetworkEndpoint(transport);

            using (
                endpoint.RegisterHandler(
                    "Command.Execute",
                    delegate(NetworkReceiveContext context)
                    {
                        context.RelayMode =
                            NetworkRelayMode.ToOthers;
                    }
                )
            )
            {
                bool dispatched =
                    endpoint.Receive(
                        new NetworkEnvelope(
                            "Command.Execute",
                            999UL,
                            false,
                            new byte[] { 4 }
                        ),
                        200UL,
                        false,
                        out NetworkReceiveContext context
                    );

                Assert.True(dispatched);
                Assert.True(
                    context.OriginalSenderWasCorrected
                );
            }

            RelayRecord relay =
                Assert.Single(transport.OtherMessages);

            Assert.Equal(200UL, relay.ExcludedPeerId);
            Assert.Equal(200UL, relay.Envelope.OriginalSenderId);
            Assert.True(relay.Envelope.IsRelay);
        }
        [Fact]
        public void Receive_ServerRelayUnreliable_UsesSelectedDelivery()
        {
            var transport = new RecordingTransport(true, 500UL);
            var endpoint = new NetworkEndpoint(transport);

            using (
                endpoint.RegisterHandler(
                    "Physics.State",
                    delegate(NetworkReceiveContext context)
                    {
                        context.RelayMode = NetworkRelayMode.ToOthers;
                        context.RelayDeliveryMode = NetworkDeliveryMode.Unreliable;
                    }
                )
            )
            {
                endpoint.Receive(
                    new NetworkEnvelope("Physics.State", 200UL, false, new byte[] { 5 }),
                    200UL,
                    false,
                    out NetworkReceiveContext context
                );
            }

            Assert.Equal(
                NetworkDeliveryMode.Unreliable,
                Assert.Single(transport.OtherMessages).DeliveryMode
            );
        }


        [Fact]
        public void Receive_ServerReturnToSender_SendsOnlyToOrigin()
        {
            var transport = new RecordingTransport(
                true,
                500UL
            );

            var endpoint = new NetworkEndpoint(transport);

            using (
                endpoint.RegisterHandler(
                    "Command.Execute",
                    delegate(NetworkReceiveContext context)
                    {
                        context.RelayMode =
                            NetworkRelayMode.ReturnToSender;
                    }
                )
            )
            {
                endpoint.Receive(
                    new NetworkEnvelope(
                        "Command.Execute",
                        200UL,
                        false,
                        new byte[] { 5 }
                    ),
                    200UL,
                    false,
                    out NetworkReceiveContext context
                );
            }

            PeerRecord sent =
                Assert.Single(transport.PeerMessages);

            Assert.Equal(200UL, sent.PeerId);
            Assert.True(sent.Envelope.IsRelay);
        }

        [Fact]
        public void Receive_UnknownMessage_DoesNotRelay()
        {
            var transport = new RecordingTransport(
                true,
                500UL
            );

            var endpoint = new NetworkEndpoint(transport);

            bool dispatched =
                endpoint.Receive(
                    new NetworkEnvelope(
                        "Unknown.Message",
                        200UL,
                        false,
                        new byte[0]
                    ),
                    200UL,
                    false,
                    out NetworkReceiveContext context
                );

            Assert.False(dispatched);
            Assert.Null(context);
            Assert.Empty(transport.PeerMessages);
            Assert.Empty(transport.OtherMessages);
            Assert.Empty(transport.EveryoneMessages);
        }

        [Fact]
        public void SendToPlayer_Client_Throws()
        {
            var endpoint = new NetworkEndpoint(
                new RecordingTransport(
                    false,
                    101UL
                )
            );

            Assert.Throws<InvalidOperationException>(
                delegate
                {
                    endpoint.SendToPlayer(
                        "Command.Result",
                        new byte[0],
                        202UL
                    );
                }
            );
        }

        [Fact]
        public void Receive_ClientMessageNotFromServer_Throws()
        {
            var endpoint = new NetworkEndpoint(
                new RecordingTransport(
                    false,
                    101UL
                )
            );

            Assert.Throws<InvalidOperationException>(
                delegate
                {
                    endpoint.Receive(
                        new NetworkEnvelope(
                            "Command.Result",
                            202UL,
                            false,
                            new byte[0]
                        ),
                        202UL,
                        false,
                        out NetworkReceiveContext context
                    );
                }
            );
        }

        [Fact]
        public void Receive_HandlerThrows_ObservesExactExceptionAndRethrows()
        {
            var transport = new RecordingTransport(true, 500UL);
            var endpoint = new NetworkEndpoint(transport);
            var expected = new InvalidOperationException("Application handler failed.");
            Exception? observed = null;

            using (
                endpoint.RegisterHandler(
                    "Command.Execute",
                    delegate
                    {
                        throw expected;
                    }
                )
            )
            {
                InvalidOperationException thrown =
                    Assert.Throws<InvalidOperationException>(
                        delegate
                        {
                            endpoint.Receive(
                                new NetworkEnvelope("Command.Execute", 200UL, false, new byte[0]),
                                200UL,
                                false,
                                delegate(Exception exception)
                                {
                                    observed = exception;
                                },
                                out NetworkReceiveContext context
                            );
                        }
                    );

                Assert.Same(expected, thrown);
            }

            Assert.Same(expected, observed);
        }
        private sealed class RecordingTransport :
            INetworkDeliveryTransport
        {
            public bool IsServer { get; }

            public ulong LocalPeerId { get; }

            public List<NetworkEnvelope> ServerMessages { get; } =
                new List<NetworkEnvelope>();

            public List<NetworkDeliveryMode> ServerDeliveryModes { get; } =
                new List<NetworkDeliveryMode>();

            public List<PeerRecord> PeerMessages { get; } =
                new List<PeerRecord>();

            public List<RelayRecord> OtherMessages { get; } =
                new List<RelayRecord>();

            public List<NetworkEnvelope> EveryoneMessages { get; } =
                new List<NetworkEnvelope>();

            public RecordingTransport(bool isServer, ulong localPeerId)
            {
                IsServer = isServer;
                LocalPeerId = localPeerId;
            }

            public void SendToServer(NetworkEnvelope envelope)
                => SendToServer(envelope, NetworkDeliveryMode.Reliable);

            public void SendToServer(NetworkEnvelope envelope, NetworkDeliveryMode deliveryMode)
            {
                ServerMessages.Add(envelope);
                ServerDeliveryModes.Add(deliveryMode);
            }

            public void SendToPeer(NetworkEnvelope envelope, ulong peerId)
                => SendToPeer(envelope, peerId, NetworkDeliveryMode.Reliable);

            public void SendToPeer(NetworkEnvelope envelope, ulong peerId, NetworkDeliveryMode deliveryMode)
                => PeerMessages.Add(new PeerRecord(envelope, peerId, deliveryMode));

            public void SendToOthers(NetworkEnvelope envelope, ulong excludedPeerId)
                => SendToOthers(envelope, excludedPeerId, NetworkDeliveryMode.Reliable);

            public void SendToOthers(NetworkEnvelope envelope, ulong excludedPeerId, NetworkDeliveryMode deliveryMode)
                => OtherMessages.Add(new RelayRecord(envelope, excludedPeerId, deliveryMode));

            public void SendToEveryone(NetworkEnvelope envelope)
                => SendToEveryone(envelope, NetworkDeliveryMode.Reliable);

            public void SendToEveryone(NetworkEnvelope envelope, NetworkDeliveryMode deliveryMode)
                => EveryoneMessages.Add(envelope);
        }

        private sealed class LegacyRecordingTransport :
            INetworkTransport
        {
            public bool IsServer { get; }

            public ulong LocalPeerId { get; }

            public LegacyRecordingTransport(bool isServer, ulong localPeerId)
            {
                IsServer = isServer;
                LocalPeerId = localPeerId;
            }

            public void SendToServer(NetworkEnvelope envelope)
            {
            }

            public void SendToPeer(NetworkEnvelope envelope, ulong peerId)
            {
            }

            public void SendToOthers(NetworkEnvelope envelope, ulong excludedPeerId)
            {
            }

            public void SendToEveryone(NetworkEnvelope envelope)
            {
            }
        }

        private sealed class PeerRecord
        {
            public NetworkEnvelope Envelope { get; }

            public ulong PeerId { get; }

            public NetworkDeliveryMode DeliveryMode { get; }

            public PeerRecord(NetworkEnvelope envelope, ulong peerId, NetworkDeliveryMode deliveryMode)
            {
                Envelope = envelope;
                PeerId = peerId;
                DeliveryMode = deliveryMode;
            }
        }

        private sealed class RelayRecord
        {
            public NetworkEnvelope Envelope { get; }

            public ulong ExcludedPeerId { get; }

            public NetworkDeliveryMode DeliveryMode { get; }

            public RelayRecord(NetworkEnvelope envelope, ulong excludedPeerId, NetworkDeliveryMode deliveryMode)
            {
                Envelope = envelope;
                ExcludedPeerId = excludedPeerId;
                DeliveryMode = deliveryMode;
            }
        }
    }
}
