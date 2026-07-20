using System;
using System.Collections.Generic;

namespace Mz.Networking
{
    /// <summary>
    /// Registers application message handlers and dispatches validated network
    /// envelopes by message type.
    /// </summary>
    public sealed class NetworkMessageDispatcher
    {
        private readonly Dictionary<
            string,
            Action<NetworkReceiveContext>
        > _handlers;

        /// <summary>
        /// Creates an empty message dispatcher.
        /// </summary>
        public NetworkMessageDispatcher()
        {
            _handlers = new Dictionary<
                string,
                Action<NetworkReceiveContext>
            >(StringComparer.Ordinal);
        }

        /// <summary>
        /// Registers one handler for an application-defined message type.
        /// </summary>
        /// <param name="messageType">
        /// The message type handled by the callback.
        /// </param>
        /// <param name="handler">The receive callback.</param>
        /// <returns>
        /// A subscription that removes the exact registration when disposed.
        /// </returns>
        public NetworkMessageSubscription RegisterHandler(
            string messageType,
            Action<NetworkReceiveContext> handler
        )
        {
            string normalizedMessageType =
                NormalizeMessageType(messageType);

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (_handlers.ContainsKey(normalizedMessageType))
            {
                throw new InvalidOperationException(
                    "A handler is already registered for message type '"
                    + normalizedMessageType
                    + "'."
                );
            }

            _handlers.Add(
                normalizedMessageType,
                handler
            );

            return new NetworkMessageSubscription(
                this,
                normalizedMessageType,
                handler
            );
        }

        /// <summary>
        /// Attempts to dispatch a received envelope to its registered handler.
        /// </summary>
        /// <param name="envelope">The received envelope.</param>
        /// <param name="transportSenderId">
        /// The sender identity reported by the trusted transport.
        /// </param>
        /// <param name="isServer">
        /// Whether the local endpoint is the authoritative server.
        /// </param>
        /// <param name="context">
        /// Receives the completed context when a handler was found.
        /// </param>
        /// <returns>
        /// True when the message type had a registered handler; otherwise
        /// false.
        /// </returns>
        public bool TryDispatch(
            NetworkEnvelope envelope,
            ulong transportSenderId,
            bool isServer,
            out NetworkReceiveContext context
        )
        {
            if (envelope == null)
                throw new ArgumentNullException(nameof(envelope));

            Action<NetworkReceiveContext> handler;

            if (!_handlers.TryGetValue(
                    envelope.MessageType,
                    out handler
                ))
            {
                context = null;
                return false;
            }

            context = NetworkMessageProcessor.Process(
                envelope,
                transportSenderId,
                isServer,
                handler
            );

            return true;
        }

        internal void UnregisterHandler(
            string messageType,
            Action<NetworkReceiveContext> handler
        )
        {
            Action<NetworkReceiveContext> registeredHandler;

            if (!_handlers.TryGetValue(
                    messageType,
                    out registeredHandler
                ))
            {
                return;
            }

            if (!ReferenceEquals(
                    registeredHandler,
                    handler
                ))
            {
                return;
            }

            _handlers.Remove(messageType);
        }

        private static string NormalizeMessageType(
            string messageType
        )
        {
            if (string.IsNullOrWhiteSpace(messageType))
            {
                throw new ArgumentException(
                    "A message type is required.",
                    nameof(messageType)
                );
            }

            return messageType.Trim();
        }
    }
}