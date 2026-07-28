using System;
using System.Collections.Generic;
using System.Linq;

namespace Mz.Networking.SpaceEngineers
{
    /// <summary>
    /// Sends network envelopes over one fixed Space Engineers secure-message
    /// channel.
    /// </summary>
    public sealed class SpaceEngineersNetworkTransport : INetworkDeliveryTransport
    {
        private const int MaximumUnreliableMessageSize = 1024;

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
            => SendToServer(envelope, NetworkDeliveryMode.Reliable);

        /// <inheritdoc />
        public void SendToServer(NetworkEnvelope envelope, NetworkDeliveryMode deliveryMode)
        {
            var serialized = Serialize(envelope, deliveryMode);

            if (!SendToServer(serialized, deliveryMode))
                throw new InvalidOperationException("Space Engineers rejected the network message sent to the server.");
        }

        /// <inheritdoc />
        public void SendToPeer(NetworkEnvelope envelope, ulong peerId)
            => SendToPeer(envelope, peerId, NetworkDeliveryMode.Reliable);

        /// <inheritdoc />
        public void SendToPeer(NetworkEnvelope envelope, ulong peerId, NetworkDeliveryMode deliveryMode)
        {
            EnsureServer();

            var serialized = Serialize(envelope, deliveryMode);

            SendSerializedToPeer(serialized, peerId, deliveryMode);
        }

        /// <inheritdoc />
        public void SendToOthers(NetworkEnvelope envelope, ulong excludedPeerId)
            => SendToOthers(envelope, excludedPeerId, NetworkDeliveryMode.Reliable);

        /// <inheritdoc />
        public void SendToOthers(NetworkEnvelope envelope, ulong excludedPeerId, NetworkDeliveryMode deliveryMode)
        {
            EnsureServer();

            var serialized = Serialize(envelope, deliveryMode);
            var playerIds = new List<ulong>();

            _gateway.GetPlayerIds(playerIds);

            foreach (var peerId in playerIds.Where(peerId => peerId != LocalPeerId && peerId != excludedPeerId))
                SendSerializedToPeer(serialized, peerId, deliveryMode);
        }

        /// <inheritdoc />
        public void SendToEveryone(NetworkEnvelope envelope)
            => SendToEveryone(envelope, NetworkDeliveryMode.Reliable);

        /// <inheritdoc />
        public void SendToEveryone(NetworkEnvelope envelope, NetworkDeliveryMode deliveryMode)
        {
            EnsureServer();

            var serialized = Serialize(envelope, deliveryMode);
            var playerIds = new List<ulong>();

            _gateway.GetPlayerIds(playerIds);

            foreach (var peerId in playerIds.Where(peerId => peerId != LocalPeerId))
                SendSerializedToPeer(serialized, peerId, deliveryMode);
        }

        private byte[] Serialize(NetworkEnvelope envelope, NetworkDeliveryMode deliveryMode)
        {
            if (envelope == null)
                throw new ArgumentNullException(nameof(envelope));

            EnsureDeliveryMode(deliveryMode);

            var serialized = _gateway.Serialize(envelope);

            if (serialized == null)
                throw new InvalidOperationException("The Space Engineers network gateway returned no serialized message.");

            if (deliveryMode == NetworkDeliveryMode.Unreliable && serialized.Length > MaximumUnreliableMessageSize)
                throw new InvalidOperationException($"An unreliable Space Engineers network message cannot exceed {MaximumUnreliableMessageSize} bytes.");

            return serialized;
        }

        private bool SendToServer(byte[] serialized, NetworkDeliveryMode deliveryMode)
        {
            var deliveryGateway = _gateway as ISpaceEngineersNetworkDeliveryGateway;

            if (deliveryGateway != null)
                return deliveryGateway.SendToServer(_channelId, serialized, deliveryMode == NetworkDeliveryMode.Reliable);

            EnsureLegacyGatewaySupports(deliveryMode);
            return _gateway.SendToServer(_channelId, serialized);
        }

        private void SendSerializedToPeer(byte[] serialized, ulong peerId, NetworkDeliveryMode deliveryMode)
        {
            var deliveryGateway = _gateway as ISpaceEngineersNetworkDeliveryGateway;
            bool sent;

            if (deliveryGateway != null)
                sent = deliveryGateway.SendToPeer(_channelId, serialized, peerId, deliveryMode == NetworkDeliveryMode.Reliable);
            else
            {
                EnsureLegacyGatewaySupports(deliveryMode);
                sent = _gateway.SendToPeer(_channelId, serialized, peerId);
            }

            if (!sent)
                throw new InvalidOperationException($"Space Engineers rejected the network message sent to peer {peerId}.");
        }

        private static void EnsureDeliveryMode(NetworkDeliveryMode deliveryMode)
        {
            if (deliveryMode < NetworkDeliveryMode.Reliable || deliveryMode > NetworkDeliveryMode.Unreliable)
                throw new ArgumentException("The delivery mode is outside the supported range.", nameof(deliveryMode));
        }

        private static void EnsureLegacyGatewaySupports(NetworkDeliveryMode deliveryMode)
        {
            if (deliveryMode != NetworkDeliveryMode.Reliable)
                throw new NotSupportedException("The configured Space Engineers network gateway does not support explicit unreliable delivery.");
        }

        private void EnsureServer()
        {
            if (!IsServer)
                throw new InvalidOperationException("Only the authoritative server can send network messages directly to players.");
        }
    }
}
