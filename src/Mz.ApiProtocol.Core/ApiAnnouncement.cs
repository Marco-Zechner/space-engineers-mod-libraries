using System;
using System.Collections.Generic;

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
        /// Gets the request correlation identifier.
        /// Guid.Empty indicates an unsolicited announcement.
        /// </summary>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Gets the API endpoints exposed by the provider.
        /// </summary>
        public IReadOnlyDictionary<string, Delegate> Endpoints { get; }

        /// <summary>
        /// Creates an API announcement.
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="correlationId"></param>
        /// <param name="endpoints"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public ApiAnnouncement(
            ApiDescriptor descriptor,
            Guid correlationId,
            IDictionary<string, Delegate> endpoints
        )
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));

            if (endpoints == null)
                throw new ArgumentNullException(nameof(endpoints));

            Descriptor = descriptor;
            CorrelationId = correlationId;
            Endpoints = CopyEndpoints(endpoints);
        }

        private static IReadOnlyDictionary<string, Delegate> CopyEndpoints(
            IDictionary<string, Delegate> endpoints
        )
        {
            var copy = new Dictionary<string, Delegate>(
                StringComparer.Ordinal
            );

            foreach (KeyValuePair<string, Delegate> pair in endpoints)
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

                copy.Add(
                    normalizedName,
                    pair.Value
                );
            }

            return copy;
        }
    }
}