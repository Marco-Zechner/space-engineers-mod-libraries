using System;
using System.Collections.Generic;
using Mz.SemanticVersioning;

namespace Mz.ApiProtocol
{
    /// <summary>
    /// Encodes and decodes API discovery messages using runtime types shared
    /// safely between separately compiled mods.
    /// </summary>
    public static class ApiDiscoveryWireProtocol
    {
        /// <summary>
        /// Gets the discovery-request wire marker.
        /// </summary>
        public const string RequestMarker =
            "Mz.ApiProtocol.Discovery.Request/1";

        /// <summary>
        /// Gets the provider-announcement wire marker.
        /// </summary>
        public const string AnnouncementMarker =
            "Mz.ApiProtocol.Discovery.Announcement/1";

        /// <summary>
        /// Gets the provider-withdrawal wire marker.
        /// </summary>
        public const string WithdrawalMarker =
            "Mz.ApiProtocol.Discovery.Withdrawal/1";

        /// <summary>
        /// Creates a transport-safe discovery request.
        /// </summary>
        /// <param name="apiId">The requested API identifier.</param>
        /// <param name="correlationId">
        /// The non-empty request correlation identifier.
        /// </param>
        /// <returns>The transport-safe request payload.</returns>
        public static object CreateRequest(
            string apiId,
            Guid correlationId
        )
        {
            var request = new ApiDiscoveryRequest(
                apiId,
                correlationId
            );

            return new object[]
            {
                RequestMarker,
                request.WireProtocolVersion.ToString(),
                request.LibraryVersion.ToString(),
                request.ApiId,
                request.CorrelationId
            };
        }

        /// <summary>
        /// Attempts to decode a discovery request.
        /// </summary>
        /// <param name="payload">The received transport payload.</param>
        /// <param name="request">
        /// Receives the parsed request when decoding succeeds.
        /// </param>
        /// <returns>True when decoding succeeds; otherwise false.</returns>
        public static bool TryParseRequest(
            object payload,
            out ApiDiscoveryRequest request
        )
        {
            request = null;

            object[] fields = payload as object[];

            if (fields == null || fields.Length < 5)
                return false;

            if (!HasMarker(fields, RequestMarker))
                return false;

            SemanticVersion wireVersion;
            SemanticVersion libraryVersion;

            if (!TryParseVersions(
                fields[1],
                fields[2],
                out wireVersion,
                out libraryVersion
            ))
            {
                return false;
            }

            string apiId = fields[3] as string;

            if (string.IsNullOrWhiteSpace(apiId))
                return false;

            if (!(fields[4] is Guid))
                return false;

            Guid correlationId = (Guid)fields[4];

            if (correlationId == Guid.Empty)
                return false;

            request = new ApiDiscoveryRequest(
                apiId,
                correlationId,
                wireVersion,
                libraryVersion
            );

            return true;
        }

        /// <summary>
        /// Creates a transport-safe provider announcement.
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
        /// The provider endpoint delegates.
        /// </param>
        /// <returns>The transport-safe announcement payload.</returns>
        public static object CreateAnnouncement(
            ApiDescriptor descriptor,
            Guid providerInstanceId,
            Guid correlationId,
            IDictionary<string, Delegate> endpoints
        )
        {
            var announcement = new ApiAnnouncement(
                descriptor,
                providerInstanceId,
                correlationId,
                endpoints
            );

            return new object[]
            {
                AnnouncementMarker,
                announcement.WireProtocolVersion.ToString(),
                announcement.LibraryVersion.ToString(),
                announcement.Descriptor.ApiId,
                announcement.Descriptor.Version.ToString(),
                announcement.ProviderInstanceId,
                announcement.CorrelationId,
                CopyEndpointPayload(announcement.Endpoints)
            };
        }

        /// <summary>
        /// Attempts to decode a provider announcement.
        /// </summary>
        /// <param name="payload">The received transport payload.</param>
        /// <param name="announcement">
        /// Receives the parsed announcement when decoding succeeds.
        /// </param>
        /// <returns>True when decoding succeeds; otherwise false.</returns>
        public static bool TryParseAnnouncement(
            object payload,
            out ApiAnnouncement announcement
        )
        {
            announcement = null;

            object[] fields = payload as object[];

            if (fields == null || fields.Length < 8)
                return false;

            if (!HasMarker(fields, AnnouncementMarker))
                return false;

            SemanticVersion wireVersion;
            SemanticVersion libraryVersion;

            if (!TryParseVersions(
                fields[1],
                fields[2],
                out wireVersion,
                out libraryVersion
            ))
            {
                return false;
            }

            string apiId = fields[3] as string;
            string apiVersionText = fields[4] as string;

            if (string.IsNullOrWhiteSpace(apiId))
                return false;

            SemanticVersion apiVersion;

            if (!SemanticVersion.TryParse(
                apiVersionText,
                out apiVersion
            ))
            {
                return false;
            }

            if (!(fields[5] is Guid))
                return false;

            Guid providerInstanceId = (Guid)fields[5];

            if (providerInstanceId == Guid.Empty)
                return false;

            if (!(fields[6] is Guid))
                return false;

            var endpoints =
                fields[7] as IDictionary<string, Delegate>;

            if (endpoints == null)
                return false;

            try
            {
                announcement = new ApiAnnouncement(
                    new ApiDescriptor(apiId, apiVersion),
                    providerInstanceId,
                    (Guid)fields[6],
                    wireVersion,
                    libraryVersion,
                    endpoints
                );

                return true;
            }
            catch (ArgumentException)
            {
                announcement = null;
                return false;
            }
        }

        /// <summary>
        /// Creates a transport-safe provider withdrawal.
        /// </summary>
        /// <param name="apiId">The withdrawn API identifier.</param>
        /// <param name="providerInstanceId">
        /// The non-empty provider-instance identity.
        /// </param>
        /// <returns>The transport-safe withdrawal payload.</returns>
        public static object CreateWithdrawal(
            string apiId,
            Guid providerInstanceId
        )
        {
            var withdrawal = new ApiProviderWithdrawal(
                apiId,
                providerInstanceId
            );

            return new object[]
            {
                WithdrawalMarker,
                withdrawal.WireProtocolVersion.ToString(),
                withdrawal.LibraryVersion.ToString(),
                withdrawal.ApiId,
                withdrawal.ProviderInstanceId
            };
        }

        /// <summary>
        /// Attempts to decode a provider withdrawal.
        /// </summary>
        /// <param name="payload">The received transport payload.</param>
        /// <param name="withdrawal">
        /// Receives the parsed withdrawal when decoding succeeds.
        /// </param>
        /// <returns>True when decoding succeeds; otherwise false.</returns>
        public static bool TryParseWithdrawal(
            object payload,
            out ApiProviderWithdrawal withdrawal
        )
        {
            withdrawal = null;

            object[] fields = payload as object[];

            if (fields == null || fields.Length < 5)
                return false;

            if (!HasMarker(fields, WithdrawalMarker))
                return false;

            SemanticVersion wireVersion;
            SemanticVersion libraryVersion;

            if (!TryParseVersions(
                fields[1],
                fields[2],
                out wireVersion,
                out libraryVersion
            ))
            {
                return false;
            }

            string apiId = fields[3] as string;

            if (string.IsNullOrWhiteSpace(apiId))
                return false;

            if (!(fields[4] is Guid))
                return false;

            Guid providerInstanceId = (Guid)fields[4];

            if (providerInstanceId == Guid.Empty)
                return false;

            withdrawal = new ApiProviderWithdrawal(
                apiId,
                providerInstanceId,
                wireVersion,
                libraryVersion
            );

            return true;
        }

        private static bool HasMarker(
            object[] fields,
            string expectedMarker
        )
        {
            return fields.Length > 0
                && string.Equals(
                    fields[0] as string,
                    expectedMarker,
                    StringComparison.Ordinal
                );
        }

        private static bool TryParseVersions(
            object wireVersionField,
            object libraryVersionField,
            out SemanticVersion wireVersion,
            out SemanticVersion libraryVersion
        )
        {
            wireVersion = null;
            libraryVersion = null;

            return SemanticVersion.TryParse(
                       wireVersionField as string,
                       out wireVersion
                   )
                && SemanticVersion.TryParse(
                       libraryVersionField as string,
                       out libraryVersion
                   );
        }

        private static Dictionary<string, Delegate>
            CopyEndpointPayload(
                IReadOnlyDictionary<string, Delegate> endpoints
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
                copy.Add(pair.Key, pair.Value);
            }

            return copy;
        }
    }
}