using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Mz.ApiProtocol
{
    /// <summary>
    /// Represents an accepted connection to a compatible API provider.
    /// </summary>
    public sealed class ApiConnection
    {
        /// <summary>
        /// Gets the identity and version exposed by the provider.
        /// </summary>
        public ApiDescriptor Descriptor { get; }

        /// <summary>
        /// Gets the identity of the connected provider instance.
        /// </summary>
        /// <remarks>
        /// <see cref="Guid.Empty"/> identifies a legacy provider whose
        /// instance identity is unavailable.
        /// </remarks>
        public Guid ProviderInstanceId { get; }

        /// <summary>
        /// Gets the endpoint delegates exposed by the provider.
        /// </summary>
        public IReadOnlyDictionary<string, Delegate> Endpoints { get; }

        /// <summary>
        /// Creates a legacy API connection without provider identity.
        /// </summary>
        /// <param name="descriptor">
        /// The identity and version exposed by the provider.
        /// </param>
        /// <param name="endpoints">
        /// The endpoint delegates exposed by the provider.
        /// </param>
        public ApiConnection(
            ApiDescriptor descriptor,
            IDictionary<string, Delegate> endpoints
        )
            : this(
                descriptor,
                Guid.Empty,
                endpoints
            )
        {
        }

        /// <summary>
        /// Creates an accepted API connection.
        /// </summary>
        /// <param name="descriptor">
        /// The identity and version exposed by the provider.
        /// </param>
        /// <param name="providerInstanceId">
        /// The provider-instance identity, or <see cref="Guid.Empty"/> for a
        /// legacy provider.
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
        public ApiConnection(
            ApiDescriptor descriptor,
            Guid providerInstanceId,
            IDictionary<string, Delegate> endpoints
        )
        {
            var validated = new ApiAnnouncement(
                descriptor,
                providerInstanceId,
                Guid.Empty,
                endpoints
            );

            Descriptor = validated.Descriptor;
            ProviderInstanceId = validated.ProviderInstanceId;

            var copy = new Dictionary<string, Delegate>(
                StringComparer.Ordinal
            );

            foreach (
                KeyValuePair<string, Delegate> pair
                in validated.Endpoints
            )
            {
                copy.Add(pair.Key, pair.Value);
            }

            Endpoints =
                new ReadOnlyDictionary<string, Delegate>(copy);
        }

        /// <summary>
        /// Attempts to retrieve an endpoint using its expected delegate type.
        /// </summary>
        /// <typeparam name="TDelegate">
        /// The expected delegate type.
        /// </typeparam>
        /// <param name="endpointName">
        /// The case-sensitive endpoint name.
        /// </param>
        /// <param name="endpoint">
        /// Receives the endpoint when it exists and has the expected type.
        /// </param>
        /// <returns>
        /// True when the endpoint exists with the expected type; otherwise
        /// false.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the endpoint name is empty or
        /// <typeparamref name="TDelegate"/> is not a delegate type.
        /// </exception>
        public bool TryGetEndpoint<TDelegate>(
            string endpointName,
            out TDelegate endpoint
        )
            where TDelegate : class
        {
            if (string.IsNullOrWhiteSpace(endpointName))
            {
                throw new ArgumentException(
                    "An endpoint name is required.",
                    nameof(endpointName)
                );
            }

            if (!typeof(Delegate).IsAssignableFrom(
                typeof(TDelegate)
            ))
            {
                throw new ArgumentException(
                    "The requested endpoint type must be a delegate.",
                    nameof(TDelegate)
                );
            }

            Delegate value;

            if (!Endpoints.TryGetValue(
                endpointName.Trim(),
                out value
            ))
            {
                endpoint = null;
                return false;
            }

            endpoint = value as TDelegate;
            return endpoint != null;
        }
    }
}