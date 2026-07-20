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

        private sealed class RecordingTransport :
            INetworkTransport
        {
            public bool IsServer { get; }

            public ulong LocalPeerId { get; }

            public List<NetworkEnvelope> ServerMessages { get; } =
                new List<NetworkEnvelope>();

            public List<PeerRecord> PeerMessages { get; } =
                new List<PeerRecord>();

            public List<RelayRecord> OtherMessages { get; } =
                new List<RelayRecord>();

            public List<NetworkEnvelope> EveryoneMessages { get; } =
                new List<NetworkEnvelope>();

            public RecordingTransport(
                bool isServer,
                ulong localPeerId
            )
            {
                IsServer = isServer;
                LocalPeerId = localPeerId;
            }

            public void SendToServer(NetworkEnvelope envelope)
            {
                ServerMessages.Add(envelope);
            }

            public void SendToPeer(
                NetworkEnvelope envelope,
                ulong peerId
            )
            {
                PeerMessages.Add(
                    new PeerRecord(
                        envelope,
                        peerId
                    )
                );
            }

            public void SendToOthers(
                NetworkEnvelope envelope,
                ulong excludedPeerId
            )
            {
                OtherMessages.Add(
                    new RelayRecord(
                        envelope,
                        excludedPeerId
                    )
                );
            }

            public void SendToEveryone(
                NetworkEnvelope envelope
            )
            {
                EveryoneMessages.Add(envelope);
            }
        }

        private sealed class PeerRecord
        {
            public NetworkEnvelope Envelope { get; }

            public ulong PeerId { get; }

            public PeerRecord(
                NetworkEnvelope envelope,
                ulong peerId
            )
            {
                Envelope = envelope;
                PeerId = peerId;
            }
        }

        private sealed class RelayRecord
        {
            public NetworkEnvelope Envelope { get; }

            public ulong ExcludedPeerId { get; }

            public RelayRecord(
                NetworkEnvelope envelope,
                ulong excludedPeerId
            )
            {
                Envelope = envelope;
                ExcludedPeerId = excludedPeerId;
            }
        }
    }
}
