using System;
using System.Collections.Generic;
using System.Linq;

namespace Mz.Networking.SpaceEngineers
{
    /// <summary>
    /// Sends network envelopes over one fixed Space Engineers secure-message
    /// channel.
    /// </summary>
    public sealed class SpaceEngineersNetworkTransport : INetworkTransport
    {
        private readonly ISpaceEngineersNetworkGateway _gateway;
        private readonly ushort _channelId;

        /// <summary>
        /// Creates a transport over one secure-message channel.
        /// </summary>
        public SpaceEngineersNetworkTransport(ISpaceEngineersNetworkGateway gateway, ushort channelId)
        {
            if (gateway == null)
                throw new ArgumentNullException(nameof(gateway));

            _gateway = gateway;
            _channelId = channelId;
        }

        /// <inheritdoc />
        public bool IsServer => _gateway.IsServer;

        /// <inheritdoc />
        public ulong LocalPeerId => _gateway.LocalPeerId;

        /// <inheritdoc />
        public void SendToServer(NetworkEnvelope envelope)
        {
            var serialized = Serialize(envelope);

            if (!_gateway.SendToServer(_channelId, serialized))
                throw new InvalidOperationException("Space Engineers rejected the network message sent to the server.");
        }

        /// <inheritdoc />
        public void SendToPeer(NetworkEnvelope envelope, ulong peerId)
        {
            EnsureServer();

            var serialized = Serialize(envelope);
            SendSerializedToPeer(serialized, peerId);
        }

        /// <inheritdoc />
        public void SendToOthers(NetworkEnvelope envelope, ulong excludedPeerId)
        {
            EnsureServer();

            var serialized = Serialize(envelope);
            var playerIds = new List<ulong>();

            _gateway.GetPlayerIds(playerIds);

            foreach (var peerId in playerIds.Where(peerId => peerId != LocalPeerId && peerId != excludedPeerId))
                SendSerializedToPeer(serialized, peerId);
        }

        /// <inheritdoc />
        public void SendToEveryone(NetworkEnvelope envelope)
        {
            EnsureServer();

            var serialized = Serialize(envelope);
            var playerIds = new List<ulong>();

            _gateway.GetPlayerIds(playerIds);

            foreach (var peerId in playerIds.Where(peerId => peerId != LocalPeerId))
                SendSerializedToPeer(serialized, peerId);
        }

        private byte[] Serialize(NetworkEnvelope envelope)
        {
            if (envelope == null)
                throw new ArgumentNullException(nameof(envelope));

            var serialized =
                _gateway.Serialize(envelope);

            if (serialized == null)
                throw new InvalidOperationException("The Space Engineers network gateway returned no serialized message.");

            return serialized;
        }

        private void SendSerializedToPeer(byte[] serialized, ulong peerId)
        {
            if (!_gateway.SendToPeer(_channelId, serialized, peerId))
                throw new InvalidOperationException($"Space Engineers rejected the network message sent to peer {peerId}.");
        }

        private void EnsureServer()
        {
            if (!IsServer)
                throw new InvalidOperationException("Only the authoritative server can send network messages directly to players.");
        }
    }
}
