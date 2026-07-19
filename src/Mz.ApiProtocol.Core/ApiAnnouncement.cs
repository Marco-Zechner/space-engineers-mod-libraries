using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mz.SemanticVersioning;

namespace Mz.ApiProtocol
{
    /// <summary>
    /// Represents a parsed provider announcement.
    /// </summary>
    public sealed class ApiAnnouncement
    {
        /// <summary>
        /// Gets the provider API identity and version.
        /// </summary>
        public ApiDescriptor Descriptor { get; }

        /// <summary>
        /// Gets the identity of the provider instance.
        /// </summary>
        public Guid ProviderInstanceId { get; }

        /// <summary>
        /// Gets the request correlation identifier.
        /// </summary>
        /// <remarks>
        /// <see cref="Guid.Empty"/> indicates an unsolicited announcement.
        /// </remarks>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Gets the provider's wire-protocol version.
        /// </summary>
        public SemanticVersion WireProtocolVersion { get; }

        /// <summary>
        /// Gets the protocol-library version embedded by the provider.
        /// </summary>
        public SemanticVersion LibraryVersion { get; }

        /// <summary>
        /// Gets the API endpoints exposed by the provider.
        /// </summary>
        public IReadOnlyDictionary<string, Delegate> Endpoints { get; }

        /// <summary>
        /// Creates an announcement using the current library and wire
        /// versions.
        /// </summary>
        /// <param name="descriptor">
        /// The provider API identity and version.
        /// </param>
        /// <param name="providerInstanceId">
        /// The non-empty provider-instance identity.
        /// </param>
        /// <param name="correlationId">
        /// The request correlation identifier, or <see cref="Guid.Empty"/>
        /// for an unsolicited announcement.
        /// </param>
        /// <param name="endpoints">
        /// The endpoint delegates exposed by the provider.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the provider identity or an endpoint is invalid.
        /// </exception>
        public ApiAnnouncement(
            ApiDescriptor descriptor,
            Guid providerInstanceId,
            Guid correlationId,
            IDictionary<string, Delegate> endpoints
        )
            : this(
                descriptor,
                providerInstanceId,
                correlationId,
                ApiProtocolInfo.WireProtocolVersion,
                ApiProtocolInfo.LibraryVersion,
                endpoints
            )
        {
        }

        internal ApiAnnouncement(
            ApiDescriptor descriptor,
            Guid providerInstanceId,
            Guid correlationId,
            SemanticVersion wireProtocolVersion,
            SemanticVersion libraryVersion,
            IDictionary<string, Delegate> endpoints
        )
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));

            if (providerInstanceId == Guid.Empty)
            {
                throw new ArgumentException(
                    "A provider requires a non-empty instance identifier.",
                    nameof(providerInstanceId)
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

            if (endpoints == null)
                throw new ArgumentNullException(nameof(endpoints));

            Descriptor = descriptor;
            ProviderInstanceId = providerInstanceId;
            CorrelationId = correlationId;
            WireProtocolVersion = wireProtocolVersion;
            LibraryVersion = libraryVersion;
            Endpoints = CopyEndpoints(endpoints);
        }

        private static IReadOnlyDictionary<string, Delegate>
            CopyEndpoints(
                IDictionary<string, Delegate> endpoints
            )
        {
            var copy = new Dictionary<string, Delegate>(
                StringComparer.Ordinal
            );

            foreach (
                KeyValuePair<string, Delegate> pair
                in endpoints
            )
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    throw new ArgumentException(
                        "API endpoint names cannot be empty.",
                        nameof(endpoints)
                    );
                }

                if (pair.Value == null)
                {
                    throw new ArgumentException(
                        "API endpoints cannot contain null delegates.",
                        nameof(endpoints)
                    );
                }

                string normalizedName = pair.Key.Trim();

                if (copy.ContainsKey(normalizedName))
                {
                    throw new ArgumentException(
                        "API endpoint names must be unique after trimming.",
                        nameof(endpoints)
                    );
                }

                copy.Add(normalizedName, pair.Value);
            }

            return new ReadOnlyDictionary<string, Delegate>(
                copy
            );
        }
    }
}