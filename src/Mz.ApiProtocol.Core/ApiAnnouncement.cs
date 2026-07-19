using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
        /// <remarks>
        /// <see cref="Guid.Empty"/> identifies a legacy announcement that did
        /// not expose provider-instance identity.
        /// </remarks>
        public Guid ProviderInstanceId { get; }

        /// <summary>
        /// Gets the request correlation identifier.
        /// </summary>
        /// <remarks>
        /// <see cref="Guid.Empty"/> indicates an unsolicited announcement.
        /// </remarks>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Gets the API endpoints exposed by the provider.
        /// </summary>
        public IReadOnlyDictionary<string, Delegate> Endpoints { get; }

        /// <summary>
        /// Creates a legacy announcement without provider-instance identity.
        /// </summary>
        /// <param name="descriptor">
        /// The provider API identity and version.
        /// </param>
        /// <param name="correlationId">
        /// The request correlation identifier, or <see cref="Guid.Empty"/>
        /// for an unsolicited announcement.
        /// </param>
        /// <param name="endpoints">
        /// The endpoint delegates exposed by the provider.
        /// </param>
        public ApiAnnouncement(
            ApiDescriptor descriptor,
            Guid correlationId,
            IDictionary<string, Delegate> endpoints
        )
            : this(
                descriptor,
                Guid.Empty,
                correlationId,
                endpoints
            )
        {
        }

        /// <summary>
        /// Creates a provider announcement.
        /// </summary>
        /// <param name="descriptor">
        /// The provider API identity and version.
        /// </param>
        /// <param name="providerInstanceId">
        /// The identity of the provider instance, or
        /// <see cref="Guid.Empty"/> for a legacy provider.
        /// </param>
        /// <param name="correlationId">
        /// The request correlation identifier, or <see cref="Guid.Empty"/>
        /// for an unsolicited announcement.
        /// </param>
        /// <param name="endpoints">
        /// The endpoint delegates exposed by the provider.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="descriptor"/> or
        /// <paramref name="endpoints"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when an endpoint name or delegate is invalid.
        /// </exception>
        public ApiAnnouncement(
            ApiDescriptor descriptor,
            Guid providerInstanceId,
            Guid correlationId,
            IDictionary<string, Delegate> endpoints
        )
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));

            if (endpoints == null)
                throw new ArgumentNullException(nameof(endpoints));

            Descriptor = descriptor;
            ProviderInstanceId = providerInstanceId;
            CorrelationId = correlationId;
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