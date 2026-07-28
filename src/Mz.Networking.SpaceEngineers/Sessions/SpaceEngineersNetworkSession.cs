using System;

namespace Mz.Networking.SpaceEngineers
{
    /// <summary>
    /// Owns one fixed Space Engineers secure-message channel and exposes its
    /// transport-independent network endpoint.
    /// </summary>
    public sealed class SpaceEngineersNetworkSession : IDisposable
    {
        private readonly ISpaceEngineersNetworkGateway _gateway;
        private readonly Action<ushort, byte[], ulong, bool> _secureMessageHandler;
        private readonly Action<SpaceEngineersNetworkReceiveFailure> _receiveFailureHandler;

        private bool _disposed;

        /// <summary>
        /// Creates a compatibility session using the legacy unframed wire.
        /// </summary>
        public SpaceEngineersNetworkSession(
            ushort channelId,
            Action<SpaceEngineersNetworkReceiveFailure> receiveFailureHandler)
            : this(
                new SpaceEngineersNetworkGateway(),
                channelId,
                null,
                false,
                receiveFailureHandler
            ) { }

        /// <summary>
        /// Creates a session using the active Space Engineers ModAPI and an
        /// explicit stable application network identity.
        /// </summary>
        public SpaceEngineersNetworkSession(
            ushort channelId,
            string networkId,
            Action<SpaceEngineersNetworkReceiveFailure> receiveFailureHandler)
            : this(
                new SpaceEngineersNetworkGateway(),
                channelId,
                networkId,
                true,
                receiveFailureHandler
            ) { }

        /// <summary>
        /// Creates a compatibility session over an explicit gateway using the
        /// legacy unframed wire.
        /// </summary>
        public SpaceEngineersNetworkSession(
            ISpaceEngineersNetworkGateway gateway,
            ushort channelId,
            Action<SpaceEngineersNetworkReceiveFailure> receiveFailureHandler)
            : this(
                gateway,
                channelId,
                null,
                false,
                receiveFailureHandler
            ) { }

        /// <summary>
        /// Creates a session over an explicit gateway and stable application
        /// network identity.
        /// </summary>
        public SpaceEngineersNetworkSession(
            ISpaceEngineersNetworkGateway gateway,
            ushort channelId,
            string networkId,
            Action<SpaceEngineersNetworkReceiveFailure> receiveFailureHandler)
            : this(
                gateway,
                channelId,
                networkId,
                true,
                receiveFailureHandler
            ) { }

        private SpaceEngineersNetworkSession(
            ISpaceEngineersNetworkGateway gateway,
            ushort channelId,
            string networkId,
            bool usesWireIdentity,
            Action<SpaceEngineersNetworkReceiveFailure> receiveFailureHandler)
        {
            if (gateway == null)
                throw new ArgumentNullException(nameof(gateway));

            if (receiveFailureHandler == null)
                throw new ArgumentNullException(nameof(receiveFailureHandler));

            _gateway = gateway;
            _receiveFailureHandler = receiveFailureHandler;
            ChannelId = channelId;
            UsesWireIdentity = usesWireIdentity;
            NetworkId = usesWireIdentity
                ? SpaceEngineersNetworkIdentity.Normalize(networkId)
                : null;

            Transport = usesWireIdentity
                ? new SpaceEngineersNetworkTransport(gateway, channelId, NetworkId)
                : new SpaceEngineersNetworkTransport(gateway, channelId);

            Endpoint = new NetworkEndpoint(Transport);
            _secureMessageHandler = ReceiveSecureMessage;

            _gateway.RegisterSecureMessageHandler(ChannelId, _secureMessageHandler);
        }

        /// <summary>
        /// Gets the owned secure-message channel.
        /// </summary>
        public ushort ChannelId { get; }

        /// <summary>
        /// Gets whether this session uses the versioned Mz.Networking wire
        /// identity rather than the legacy unframed envelope.
        /// </summary>
        public bool UsesWireIdentity { get; }

        /// <summary>
        /// Gets the stable application network identity, or null for a legacy
        /// compatibility session.
        /// </summary>
        public string NetworkId { get; }

        /// <summary>
        /// Gets the concrete Space Engineers transport.
        /// </summary>
        public SpaceEngineersNetworkTransport Transport { get; }

        /// <summary>
        /// Gets the transport-independent mod-facing endpoint.
        /// </summary>
        public NetworkEndpoint Endpoint { get; }

        /// <summary>
        /// Removes the exact secure-message handler registration.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _gateway.UnregisterSecureMessageHandler(ChannelId, _secureMessageHandler);
        }

        private void ReceiveSecureMessage(
            ushort channelId,
            byte[] serialized,
            ulong senderPeerId,
            bool senderIsServer)
        {
            if (channelId != ChannelId)
            {
                ReportFailure(
                    channelId,
                    serialized,
                    senderPeerId,
                    senderIsServer,
                    SpaceEngineersNetworkReceiveFailureKind.ProcessingFailure,
                    null,
                    new InvalidOperationException(
                        "The secure-message handler received channel "
                        + channelId
                        + " instead of its configured channel "
                        + ChannelId
                        + "."
                    )
                );

                return;
            }

            if (serialized == null)
            {
                ReportFailure(
                    channelId,
                    null,
                    senderPeerId,
                    senderIsServer,
                    SpaceEngineersNetworkReceiveFailureKind.ProcessingFailure,
                    null,
                    new ArgumentNullException(nameof(serialized))
                );

                return;
            }

            var serializedEnvelope = serialized;
            string observedNetworkId = null;

            if (UsesWireIdentity)
            {
                var decoded =
                    SpaceEngineersNetworkWireCodec.Decode(
                        serialized,
                        NetworkId
                    );

                if (decoded.Status != SpaceEngineersNetworkWireStatus.Success)
                {
                    ReportFailure(
                        channelId,
                        serialized,
                        senderPeerId,
                        senderIsServer,
                        ToFailureKind(decoded.Status),
                        decoded.ObservedNetworkId,
                        decoded.Exception
                    );

                    return;
                }

                serializedEnvelope = decoded.SerializedEnvelope;
                observedNetworkId = decoded.ObservedNetworkId;
            }

            NetworkEnvelope envelope;

            try
            {
                envelope = _gateway.Deserialize(serializedEnvelope);

                if (envelope == null)
                    throw new InvalidOperationException("The serialized network envelope was empty.");
            }
            catch (Exception exception)
            {
                ReportFailure(
                    channelId,
                    serialized,
                    senderPeerId,
                    senderIsServer,
                    SpaceEngineersNetworkReceiveFailureKind.MalformedOwnPacket,
                    observedNetworkId,
                    exception
                );

                return;
            }

            Exception handlerFailure = null;

            try
            {
                NetworkReceiveContext ignored;

                Endpoint.Receive(
                    envelope,
                    senderPeerId,
                    senderIsServer,
                    delegate(Exception exception)
                    {
                        handlerFailure = exception;
                    },
                    out ignored
                );
            }
            catch (Exception exception)
            {
                var kind = ReferenceEquals(handlerFailure, exception)
                    ? SpaceEngineersNetworkReceiveFailureKind.HandlerFailure
                    : SpaceEngineersNetworkReceiveFailureKind.ProcessingFailure;

                ReportFailure(
                    channelId,
                    serialized,
                    senderPeerId,
                    senderIsServer,
                    kind,
                    observedNetworkId,
                    exception
                );
            }
        }

        private void ReportFailure(
            ushort channelId,
            byte[] serialized,
            ulong senderPeerId,
            bool senderIsServer,
            SpaceEngineersNetworkReceiveFailureKind kind,
            string observedNetworkId,
            Exception exception)
        {
            _receiveFailureHandler(
                new SpaceEngineersNetworkReceiveFailure(
                    channelId,
                    serialized ?? Array.Empty<byte>(),
                    senderPeerId,
                    senderIsServer,
                    kind,
                    NetworkId,
                    observedNetworkId,
                    exception
                )
            );
        }

        private static SpaceEngineersNetworkReceiveFailureKind ToFailureKind(
            SpaceEngineersNetworkWireStatus status)
        {
            switch (status)
            {
                case SpaceEngineersNetworkWireStatus.ForeignPacket:
                    return SpaceEngineersNetworkReceiveFailureKind.ForeignPacket;

                case SpaceEngineersNetworkWireStatus.NetworkMismatch:
                    return SpaceEngineersNetworkReceiveFailureKind.NetworkMismatch;

                case SpaceEngineersNetworkWireStatus.UnsupportedWireVersion:
                    return SpaceEngineersNetworkReceiveFailureKind.UnsupportedWireVersion;

                case SpaceEngineersNetworkWireStatus.MalformedWirePacket:
                    return SpaceEngineersNetworkReceiveFailureKind.MalformedWirePacket;

                default:
                    throw new InvalidOperationException("A successful wire result cannot be converted to a receive failure.");
            }
        }
    }
}
