using System;

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
        /// Creates a discovery request.
        /// </summary>
        /// <param name="apiId"></param>
        /// <param name="correlationId"></param>
        /// <exception cref="ArgumentException"></exception>
        public ApiDiscoveryRequest(
            string apiId,
            Guid correlationId
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

            ApiId = apiId.Trim();
            CorrelationId = correlationId;
        }
    }
}