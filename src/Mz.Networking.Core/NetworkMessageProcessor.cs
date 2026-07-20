using System;

namespace Mz.Networking
{
    /// <summary>
    /// Applies transport-independent receive validation before dispatching a
    /// network message to application code.
    /// </summary>
    public static class NetworkMessageProcessor
    {
        /// <summary>
        /// Validates a received envelope and invokes its application handler.
        /// </summary>
        /// <param name="envelope">The received message envelope.</param>
        /// <param name="transportSenderId">
        /// The sender identity reported by the trusted transport.
        /// </param>
        /// <param name="isServer">
        /// Whether processing occurs on the authoritative server.
        /// </param>
        /// <param name="handler">
        /// The application handler that may select relay behavior.
        /// </param>
        /// <returns>The completed receive context.</returns>
        public static NetworkReceiveContext Process(
            NetworkEnvelope envelope,
            ulong transportSenderId,
            bool isServer,
            Action<NetworkReceiveContext> handler
        )
        {
            if (envelope == null)
                throw new ArgumentNullException(nameof(envelope));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            bool senderWasCorrected =
                isServer
                && envelope.OriginalSenderId
                    != transportSenderId;

            NetworkEnvelope validatedEnvelope =
                senderWasCorrected
                    ? envelope.WithOriginalSenderId(
                        transportSenderId
                    )
                    : envelope;

            var context = new NetworkReceiveContext(
                validatedEnvelope,
                transportSenderId,
                isServer,
                senderWasCorrected
            );

            handler(context);

            ValidateRelayMode(context.RelayMode);

            return context;
        }

        private static void ValidateRelayMode(
            NetworkRelayMode relayMode
        )
        {
            if (relayMode < NetworkRelayMode.None
                || relayMode
                    > NetworkRelayMode.ReturnToSender)
            {
                throw new InvalidOperationException(
                    "The receive handler selected an unsupported "
                    + "relay mode."
                );
            }
        }
    }
}