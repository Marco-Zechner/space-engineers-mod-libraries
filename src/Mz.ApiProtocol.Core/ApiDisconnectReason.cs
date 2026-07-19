namespace Mz.ApiProtocol
{
    /// <summary>
    /// Describes why an accepted API connection was removed.
    /// </summary>
    public enum ApiDisconnectReason
    {
        /// <summary>
        /// The consuming mod explicitly released the connection.
        /// </summary>
        ConsumerRequested = 0,

        /// <summary>
        /// The consuming mod released the connection before requesting
        /// provider discovery again.
        /// </summary>
        RediscoveryRequested = 1,

        /// <summary>
        /// The consumer stopped listening during its lifecycle shutdown.
        /// </summary>
        ConsumerStopped = 2
    }
}