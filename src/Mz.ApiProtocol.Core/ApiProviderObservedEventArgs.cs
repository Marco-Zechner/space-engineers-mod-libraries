using System;

namespace Mz.ApiProtocol
{
    /// <summary>
    /// Provides information about an API provider observed during discovery.
    /// </summary>
    public sealed class ApiProviderObservedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the identity and version announced by the provider.
        /// </summary>
        public ApiDescriptor Descriptor { get; }

        /// <summary>
        /// Gets the compatibility result for the observed provider.
        /// </summary>
        public ApiCompatibilityStatus CompatibilityStatus { get; }

        /// <summary>
        /// Gets the correlation identifier from the provider announcement.
        /// </summary>
        /// <remarks>
        /// <see cref="Guid.Empty"/> indicates an unsolicited announcement.
        /// </remarks>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Creates provider-observation event data.
        /// </summary>
        /// <param name="descriptor">
        /// The identity and version announced by the provider.
        /// </param>
        /// <param name="compatibilityStatus">
        /// The compatibility result for the observed provider.
        /// </param>
        /// <param name="correlationId">
        /// The correlation identifier from the provider announcement.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="descriptor"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="compatibilityStatus"/> is not a defined
        /// compatibility status.
        /// </exception>
        public ApiProviderObservedEventArgs(
            ApiDescriptor descriptor,
            ApiCompatibilityStatus compatibilityStatus,
            Guid correlationId
        )
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));

            if (compatibilityStatus
                    < ApiCompatibilityStatus.Compatible
                || compatibilityStatus
                    > ApiCompatibilityStatus.ProviderTooNew)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(compatibilityStatus)
                );
            }

            Descriptor = descriptor;
            CompatibilityStatus = compatibilityStatus;
            CorrelationId = correlationId;
        }
    }
}