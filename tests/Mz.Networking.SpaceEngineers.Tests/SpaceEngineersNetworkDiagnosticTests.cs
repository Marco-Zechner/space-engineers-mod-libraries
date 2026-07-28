using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Mz.Networking.SpaceEngineers.Tests
{
    public sealed class SpaceEngineersNetworkDiagnosticTests
    {
        private const ushort ChannelId = 41000;
        private const string NetworkId = "Example.Network";

        [Fact]
        public void DiagnosticSeverityValues_MapToMzLoggingLevels()
        {
            Assert.Equal(
                0,
                (int)SpaceEngineersNetworkDiagnosticSeverity.Trace
            );

            Assert.Equal(
                1,
                (int)SpaceEngineersNetworkDiagnosticSeverity.Debug
            );

            Assert.Equal(
                2,
                (int)SpaceEngineersNetworkDiagnosticSeverity.Information
            );

            Assert.Equal(
                3,
                (int)SpaceEngineersNetworkDiagnosticSeverity.Warning
            );

            Assert.Equal(
                4,
                (int)SpaceEngineersNetworkDiagnosticSeverity.Error
            );

            Assert.Equal(
                5,
                (int)SpaceEngineersNetworkDiagnosticSeverity.Critical
            );
        }

        [Fact]
        public void ForeignPacket_RaisesBoundedStructuredDiagnostic()
        {
            var gateway = new RecordingGateway(true, 100UL)
            {
                DiagnosticString =
                    "Serialized title\r\nwith control"
            };

            byte[] packet = Encoding.ASCII.GetBytes(
                "Foreign protocol title "
                + new string('X', 200)
            );

            SpaceEngineersNetworkReceiveFailure? callbackFailure = null;
            SpaceEngineersNetworkReceiveFailure? eventFailure = null;

            using (
                var session =
                    new SpaceEngineersNetworkSession(
                        gateway,
                        ChannelId,
                        NetworkId,
                        delegate(
                            SpaceEngineersNetworkReceiveFailure failure
                        )
                        {
                            callbackFailure = failure;
                        }
                    )
            )
            {
                session.Diagnostic +=
                    delegate(
                        SpaceEngineersNetworkReceiveFailure failure
                    )
                    {
                        eventFailure = failure;
                    };

                gateway.Deliver(
                    packet,
                    200UL,
                    false
                );
            }

            Assert.NotNull(callbackFailure);
            Assert.Same(callbackFailure, eventFailure);

            Assert.Equal(
                SpaceEngineersNetworkDiagnosticSeverity.Warning,
                callbackFailure.Severity
            );

            Assert.Equal(
                "network.receive.foreign-packet",
                callbackFailure.DiagnosticCode
            );

            Assert.Equal(packet.Length, callbackFailure.PacketLength);
            Assert.StartsWith("46 6F 72 65", callbackFailure.PacketPreview);
            Assert.Contains(
                "(+"
                + (packet.Length - 32)
                + " bytes)",
                callbackFailure.PacketPreview
            );
            Assert.True(
                callbackFailure.PacketPreview.Length < 160
            );

            Assert.Contains(
                "Serialized title with control",
                callbackFailure.DiscoveredText
            );

            Assert.InRange(
                callbackFailure.DiscoveredText.Length,
                1,
                4
            );

            foreach (
                string discovered in
                    callbackFailure.DiscoveredText
            ) {
                Assert.InRange(discovered.Length, 4, 96);
                Assert.DoesNotContain("\r", discovered);
                Assert.DoesNotContain("\n", discovered);
            }

            Assert.Contains(
                "[network.receive.foreign-packet]",
                callbackFailure.DiagnosticMessage
            );

            Assert.Contains(
                "channel=41000",
                callbackFailure.DiagnosticMessage
            );

            Assert.Contains(
                "sender=200",
                callbackFailure.DiagnosticMessage
            );

            Assert.DoesNotContain(
                "\r",
                callbackFailure.DiagnosticMessage
            );

            Assert.DoesNotContain(
                "\n",
                callbackFailure.DiagnosticMessage
            );

            Assert.InRange(
                callbackFailure.DiagnosticMessage.Length,
                1,
                1024
            );
        }

        [Fact]
        public void HandlerFailure_RaisesErrorWithoutConflictText()
        {
            var gateway = new RecordingGateway(true, 100UL)
            {
                DeserializedEnvelope =
                    new NetworkEnvelope(
                        "Command.Execute",
                        200UL,
                        false,
                        new byte[] { 1 }
                    )
            };

            var expected =
                new InvalidOperationException(
                    "Application handler failed."
                );

            SpaceEngineersNetworkReceiveFailure? diagnostic = null;

            using (
                var session =
                    new SpaceEngineersNetworkSession(
                        gateway,
                        ChannelId,
                        delegate
                        {
                        }
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
                session.Diagnostic +=
                    delegate(
                        SpaceEngineersNetworkReceiveFailure failure
                    )
                    {
                        diagnostic = failure;
                    };

                gateway.Deliver(
                    new byte[]
                    {
                        65,
                        66,
                        67,
                        68
                    },
                    200UL,
                    false
                );
            }

            Assert.NotNull(diagnostic);

            Assert.Equal(
                SpaceEngineersNetworkDiagnosticSeverity.Error,
                diagnostic.Severity
            );

            Assert.Equal(
                "network.receive.handler-failure",
                diagnostic.DiagnosticCode
            );

            Assert.False(diagnostic.IsChannelConflict);
            Assert.Empty(diagnostic.DiscoveredText);
            Assert.Same(expected, diagnostic.Exception);
        }

        [Fact]
        public void ConstructorWithoutCallback_PublishesDiagnosticEvent()
        {
            var gateway = new RecordingGateway(true, 100UL);
            SpaceEngineersNetworkReceiveFailure? diagnostic = null;

            using (
                var session =
                    new SpaceEngineersNetworkSession(
                        gateway,
                        ChannelId,
                        NetworkId
                    )
            )
            {
                session.Diagnostic +=
                    delegate(
                        SpaceEngineersNetworkReceiveFailure failure
                    )
                    {
                        diagnostic = failure;
                    };

                gateway.Deliver(
                    new byte[]
                    {
                        1,
                        2,
                        3,
                        4
                    },
                    200UL,
                    false
                );
            }

            Assert.NotNull(diagnostic);

            Assert.Equal(
                SpaceEngineersNetworkReceiveFailureKind.ForeignPacket,
                diagnostic.Kind
            );
        }

        [Fact]
        public void LargeForeignPacket_SkipsStringDeserialization()
        {
            var gateway = new RecordingGateway(true, 100UL)
            {
                DiagnosticString = "Should not be inspected."
            };

            SpaceEngineersNetworkReceiveFailure? diagnostic = null;

            using (
                var session =
                    new SpaceEngineersNetworkSession(
                        gateway,
                        ChannelId,
                        NetworkId
                    )
            )
            {
                session.Diagnostic +=
                    delegate(
                        SpaceEngineersNetworkReceiveFailure failure
                    )
                    {
                        diagnostic = failure;
                    };

                gateway.Deliver(
                    new byte[513],
                    200UL,
                    false
                );
            }

            Assert.NotNull(diagnostic);
            Assert.Equal(0, gateway.TryDeserializeStringCount);
            Assert.Empty(diagnostic.DiscoveredText);
        }

        [Fact]
        public void DiagnosticMessage_SanitizesNetworkIdControls()
        {
            const string networkId =
                "Example\r\nNetwork\tId";

            var gateway = new RecordingGateway(true, 100UL);
            SpaceEngineersNetworkReceiveFailure? diagnostic = null;

            using (
                var session =
                    new SpaceEngineersNetworkSession(
                        gateway,
                        ChannelId,
                        networkId
                    )
            )
            {
                session.Diagnostic +=
                    delegate(
                        SpaceEngineersNetworkReceiveFailure failure
                    )
                    {
                        diagnostic = failure;
                    };

                gateway.Deliver(
                    new byte[]
                    {
                        1,
                        2,
                        3,
                        4
                    },
                    200UL,
                    false
                );
            }

            Assert.NotNull(diagnostic);
            Assert.Equal(networkId, diagnostic.ExpectedNetworkId);

            Assert.Contains(
                "expectedNetworkId=\"Example Network Id\"",
                diagnostic.DiagnosticMessage
            );

            Assert.DoesNotContain(
                "\r",
                diagnostic.DiagnosticMessage
            );

            Assert.DoesNotContain(
                "\n",
                diagnostic.DiagnosticMessage
            );

            Assert.DoesNotContain(
                "\t",
                diagnostic.DiagnosticMessage
            );
        }

        [Fact]
        public void DiagnosticSubscriberFailure_IsIsolated()
        {
            var gateway = new RecordingGateway(true, 100UL);
            var laterSubscriberCalled = false;

            using (
                var session =
                    new SpaceEngineersNetworkSession(
                        gateway,
                        ChannelId,
                        NetworkId,
                        delegate
                        {
                        }
                    )
            )
            {
                session.Diagnostic +=
                    delegate
                    {
                        throw new InvalidOperationException(
                            "Diagnostic sink failed."
                        );
                    };

                session.Diagnostic +=
                    delegate
                    {
                        laterSubscriberCalled = true;
                    };

                gateway.Deliver(
                    new byte[]
                    {
                        1,
                        2,
                        3,
                        4
                    },
                    200UL,
                    false
                );
            }

            Assert.True(laterSubscriberCalled);
        }

        [Fact]
        public void BinaryForeignPacket_DoesNotInventText()
        {
            var gateway = new RecordingGateway(true, 100UL);
            SpaceEngineersNetworkReceiveFailure? diagnostic = null;

            using (
                var session =
                    new SpaceEngineersNetworkSession(
                        gateway,
                        ChannelId,
                        NetworkId,
                        delegate
                        {
                        }
                    )
            )
            {
                session.Diagnostic +=
                    delegate(
                        SpaceEngineersNetworkReceiveFailure failure
                    )
                    {
                        diagnostic = failure;
                    };

                gateway.Deliver(
                    new byte[]
                    {
                        0,
                        255,
                        1,
                        254,
                        2,
                        253,
                        3,
                        252
                    },
                    200UL,
                    false
                );
            }

            Assert.NotNull(diagnostic);
            Assert.Empty(diagnostic.DiscoveredText);
        }

        private sealed class RecordingGateway :
            ISpaceEngineersNetworkDeliveryGateway,
            ISpaceEngineersNetworkDiagnosticGateway
        {
            public bool IsServer { get; }

            public ulong LocalPeerId { get; }

            public string? DiagnosticString { get; set; }

            public int TryDeserializeStringCount { get; private set; }

            public NetworkEnvelope? DeserializedEnvelope { get; set; }

            public Action<ushort, byte[], ulong, bool>?
                RegisteredHandler { get; private set; }

            public byte[] SerializedBytes { get; set; } =
                new byte[] { 1 };

            public List<ulong> PlayerIds { get; } =
                new List<ulong>();

            public RecordingGateway(
                bool isServer,
                ulong localPeerId)
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
                => DeserializedEnvelope!;

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
                => true;

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

            public void GetPlayerIds(
                List<ulong> playerIds)
                => playerIds.AddRange(PlayerIds);

            public bool TryDeserializeString(
                byte[] serialized,
                out string value)
            {
                TryDeserializeStringCount++;
                value = DiagnosticString!;
                return value != null;
            }

            public void Deliver(
                byte[] serialized,
                ulong senderPeerId,
                bool senderIsServer)
            {
                RegisteredHandler!(
                    ChannelId,
                    serialized,
                    senderPeerId,
                    senderIsServer
                );
            }
        }
    }
}
