using System;
using Mz.SemanticVersioning;

namespace Mz.ApiProtocol
{
    /// <summary>
    /// Represents a parsed request for an API provider announcement.
    /// </summary>
    public sealed class ApiDiscoveryRequest
    {
        /// <summary>
        /// Gets the requested API identifier.
        /// </summary>
        public string ApiId { get; }

        /// <summary>
        /// Gets the request correlation identifier.
        /// </summary>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Gets the wire-protocol version used by the requesting consumer.
        /// </summary>
        public SemanticVersion WireProtocolVersion { get; }

        /// <summary>
        /// Gets the protocol-library version embedded by the consumer.
        /// </summary>
        public SemanticVersion LibraryVersion { get; }

        /// <summary>
        /// Creates a request using the current library and wire versions.
        /// </summary>
        /// <param name="apiId">
        /// The case-sensitive API identifier being requested.
        /// </param>
        /// <param name="correlationId">
        /// The non-empty request correlation identifier.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when an argument is empty or invalid.
        /// </exception>
        public ApiDiscoveryRequest(
            string apiId,
            Guid correlationId
        )
            : this(
                apiId,
                correlationId,
                ApiProtocolInfo.WireProtocolVersion,
                ApiProtocolInfo.LibraryVersion
            )
        {
        }

        internal ApiDiscoveryRequest(
            string apiId,
            Guid correlationId,
            SemanticVersion wireProtocolVersion,
            SemanticVersion libraryVersion
        )
        {
            if (string.IsNullOrWhiteSpace(apiId))
            {
                throw new ArgumentException(
                    "An API identifier is required.",
                    nameof(apiId)
                );
            }

            if (correlationId == Guid.Empty)
            {
                throw new ArgumentException(
                    "A discovery request requires a non-empty "
                    + "correlation identifier.",
                    nameof(correlationId)
                );
            }

            if (wireProtocolVersion == null)
            {
                throw new ArgumentNullException(
                    nameof(wireProtocolVersion)
                );
            }

            if (libraryVersion == null)
            {
                throw new ArgumentNullException(
                    nameof(libraryVersion)
                );
            }

            ApiId = apiId.Trim();
            CorrelationId = correlationId;
            WireProtocolVersion = wireProtocolVersion;
            LibraryVersion = libraryVersion;
        }
    }
}