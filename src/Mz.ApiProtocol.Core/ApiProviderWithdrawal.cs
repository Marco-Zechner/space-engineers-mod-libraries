using System;

namespace Mz.ApiProtocol
{
    /// <summary>
    /// Represents a provider instance withdrawing an exposed API.
    /// </summary>
    public sealed class ApiProviderWithdrawal
    {
        /// <summary>
        /// Gets the withdrawn API identifier.
        /// </summary>
        public string ApiId { get; }

        /// <summary>
        /// Gets the identity of the provider instance that stopped.
        /// </summary>
        public Guid ProviderInstanceId { get; }

        /// <summary>
        /// Creates a provider-withdrawal message.
        /// </summary>
        /// <param name="apiId">
        /// The case-sensitive API identifier.
        /// </param>
        /// <param name="providerInstanceId">
        /// The non-empty identity of the provider instance.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="apiId"/> is empty or
        /// <paramref name="providerInstanceId"/> is empty.
        /// </exception>
        public ApiProviderWithdrawal(
            string apiId,
            Guid providerInstanceId
        )
        {
            if (string.IsNullOrWhiteSpace(apiId))
            {
                throw new ArgumentException(
                    "An API identifier is required.",
                    nameof(apiId)
                );
            }

            if (providerInstanceId == Guid.Empty)
            {
                throw new ArgumentException(
                    "A provider withdrawal requires a non-empty "
                    + "provider instance identifier.",
                    nameof(providerInstanceId)
                );
            }

            ApiId = apiId.Trim();
            ProviderInstanceId = providerInstanceId;
        }
    }
}