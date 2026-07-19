using System;

namespace Mz.ApiProtocol
{
    /// <summary>
    /// Provides information about an API connection that was removed.
    /// </summary>
    public sealed class ApiDisconnectedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the connection that was removed.
        /// </summary>
        public ApiConnection PreviousConnection { get; }

        /// <summary>
        /// Gets the reason the connection was removed.
        /// </summary>
        public ApiDisconnectReason Reason { get; }

        /// <summary>
        /// Creates API disconnection event data.
        /// </summary>
        /// <param name="previousConnection">
        /// The connection that was removed.
        /// </param>
        /// <param name="reason">
        /// The reason the connection was removed.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="previousConnection"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="reason"/> is not a defined reason.
        /// </exception>
        public ApiDisconnectedEventArgs(
            ApiConnection previousConnection,
            ApiDisconnectReason reason
        )
        {
            if (previousConnection == null)
            {
                throw new ArgumentNullException(
                    nameof(previousConnection)
                );
            }

            if (reason < ApiDisconnectReason.ConsumerRequested
                || reason > ApiDisconnectReason.ProviderWithdrawn)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reason)
                );
            }

            PreviousConnection = previousConnection;
            Reason = reason;
        }
    }
}