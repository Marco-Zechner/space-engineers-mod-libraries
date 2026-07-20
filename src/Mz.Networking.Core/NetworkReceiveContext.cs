using System;

namespace Mz.Networking
{
    /// <summary>
    /// Contains trusted receive metadata and mutable routing decisions for one
    /// processed network message.
    /// </summary>
    public sealed class NetworkReceiveContext
    {
        /// <summary>
        /// Gets the validated message envelope.
        /// </summary>
        public NetworkEnvelope Envelope { get; }

        /// <summary>
        /// Gets the immediate transport sender identity.
        /// </summary>
        public ulong TransportSenderId { get; }

        /// <summary>
        /// Gets whether the message is being processed by the server.
        /// </summary>
        public bool IsServer { get; }

        /// <summary>
        /// Gets whether the claimed original sender was replaced with the
        /// trusted transport sender.
        /// </summary>
        public bool OriginalSenderWasCorrected { get; }

        /// <summary>
        /// Gets or sets how the server should relay the processed message.
        /// </summary>
        public NetworkRelayMode RelayMode { get; set; }

        /// <summary>
        /// Gets or sets whether the envelope must be serialized again before
        /// it is sent onward.
        /// </summary>
        public bool RequiresSerialization { get; set; }

        internal NetworkReceiveContext(
            NetworkEnvelope envelope,
            ulong transportSenderId,
            bool isServer,
            bool originalSenderWasCorrected
        )
        {
            if (envelope == null)
                throw new ArgumentNullException(nameof(envelope));

            Envelope = envelope;
            TransportSenderId = transportSenderId;
            IsServer = isServer;
            OriginalSenderWasCorrected =
                originalSenderWasCorrected;

            RelayMode = NetworkRelayMode.None;
            RequiresSerialization =
                originalSenderWasCorrected;
        }
    }
}