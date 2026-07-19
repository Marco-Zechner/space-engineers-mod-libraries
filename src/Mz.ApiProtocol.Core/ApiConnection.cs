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
        /// Gets the endpoint delegates exposed by the provider.
        /// </summary>
        public IReadOnlyDictionary<string, Delegate> Endpoints { get; }

        /// <summary>
        /// Creates an accepted API connection.
        /// </summary>
        /// <param name="descriptor">
        /// The identity and version exposed by the provider.
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
            IDictionary<string, Delegate> endpoints
        )
        {
            var validated = new ApiAnnouncement(
                descriptor,
                Guid.Empty,
                endpoints
            );

            Descriptor = validated.Descriptor;

            var copy = new Dictionary<string, Delegate>(
                StringComparer.Ordinal
            );

            foreach (
                var pair
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
        /// The expected delegate type. Cross-mod endpoints should normally use
        /// standard <see cref="Action"/> or <see cref="Func{TResult}"/>
        /// delegate types containing only shared runtime or game types.
        /// </typeparam>
        /// <param name="endpointName">
        /// The case-sensitive endpoint name.
        /// </param>
        /// <param name="endpoint">
        /// Receives the endpoint when it exists and has the expected type.
        /// </param>
        /// <returns>
        /// True when the endpoint exists and can be assigned to
        /// <typeparamref name="TDelegate"/>; otherwise false.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="endpointName"/> is empty or
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