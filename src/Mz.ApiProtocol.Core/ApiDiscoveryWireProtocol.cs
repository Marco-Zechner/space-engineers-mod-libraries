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
        /// Gets the legacy provider-announcement wire marker.
        /// </summary>
        public const string AnnouncementMarker =
            "Mz.ApiProtocol.Discovery.Announcement/1";

        /// <summary>
        /// Gets the provider-announcement marker that includes provider
        /// instance identity.
        /// </summary>
        public const string AnnouncementMarkerV2 =
            "Mz.ApiProtocol.Discovery.Announcement/2";

        /// <summary>
        /// Gets the provider-withdrawal wire marker.
        /// </summary>
        public const string WithdrawalMarker =
            "Mz.ApiProtocol.Discovery.Withdrawal/1";

        /// <summary>
        /// Creates a transport-safe discovery request payload.
        /// </summary>
        /// <param name="apiId">
        /// The case-sensitive API identifier being requested.
        /// </param>
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
                request.ApiId,
                request.CorrelationId
            };
        }

        /// <summary>
        /// Attempts to decode a discovery request payload.
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

            if (fields == null || fields.Length != 3)
                return false;

            string marker = fields[0] as string;
            string apiId = fields[1] as string;

            if (!string.Equals(
                marker,
                RequestMarker,
                StringComparison.Ordinal
            ))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(apiId))
                return false;

            if (!(fields[2] is Guid))
                return false;

            Guid correlationId = (Guid)fields[2];

            if (correlationId == Guid.Empty)
                return false;

            request = new ApiDiscoveryRequest(
                apiId,
                correlationId
            );

            return true;
        }

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
        /// The provider endpoint delegates.
        /// </param>
        /// <returns>The transport-safe legacy announcement payload.</returns>
        public static object CreateAnnouncement(
            ApiDescriptor descriptor,
            Guid correlationId,
            IDictionary<string, Delegate> endpoints
        )
        {
            var announcement = new ApiAnnouncement(
                descriptor,
                correlationId,
                endpoints
            );

            return new object[]
            {
                AnnouncementMarker,
                announcement.Descriptor.ApiId,
                announcement.Descriptor.Version.ToString(),
                announcement.CorrelationId,
                CopyEndpointPayload(announcement.Endpoints)
            };
        }

        /// <summary>
        /// Creates an announcement containing provider-instance identity.
        /// </summary>
        /// <param name="descriptor">
        /// The provider API identity and version.
        /// </param>
        /// <param name="providerInstanceId">
        /// The non-empty provider-instance identifier.
        /// </param>
        /// <param name="correlationId">
        /// The request correlation identifier, or <see cref="Guid.Empty"/>
        /// for an unsolicited announcement.
        /// </param>
        /// <param name="endpoints">
        /// The provider endpoint delegates.
        /// </param>
        /// <returns>The transport-safe version-two announcement payload.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="providerInstanceId"/> is empty.
        /// </exception>
        public static object CreateAnnouncement(
            ApiDescriptor descriptor,
            Guid providerInstanceId,
            Guid correlationId,
            IDictionary<string, Delegate> endpoints
        )
        {
            if (providerInstanceId == Guid.Empty)
            {
                throw new ArgumentException(
                    "A version-two announcement requires a non-empty "
                    + "provider instance identifier.",
                    nameof(providerInstanceId)
                );
            }

            var announcement = new ApiAnnouncement(
                descriptor,
                providerInstanceId,
                correlationId,
                endpoints
            );

            return new object[]
            {
                AnnouncementMarkerV2,
                announcement.Descriptor.ApiId,
                announcement.Descriptor.Version.ToString(),
                announcement.ProviderInstanceId,
                announcement.CorrelationId,
                CopyEndpointPayload(announcement.Endpoints)
            };
        }

        /// <summary>
        /// Attempts to decode either a legacy or version-two announcement.
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

            if (fields == null)
                return false;

            string marker = fields.Length > 0
                ? fields[0] as string
                : null;

            if (string.Equals(
                marker,
                AnnouncementMarker,
                StringComparison.Ordinal
            ))
            {
                return TryParseLegacyAnnouncement(
                    fields,
                    out announcement
                );
            }

            if (string.Equals(
                marker,
                AnnouncementMarkerV2,
                StringComparison.Ordinal
            ))
            {
                return TryParseVersionTwoAnnouncement(
                    fields,
                    out announcement
                );
            }

            return false;
        }

        /// <summary>
        /// Creates a provider-withdrawal payload.
        /// </summary>
        /// <param name="apiId">
        /// The case-sensitive API identifier being withdrawn.
        /// </param>
        /// <param name="providerInstanceId">
        /// The non-empty provider-instance identifier.
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
                withdrawal.ApiId,
                withdrawal.ProviderInstanceId
            };
        }

        /// <summary>
        /// Attempts to decode a provider-withdrawal payload.
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

            if (fields == null || fields.Length != 3)
                return false;

            if (!string.Equals(
                fields[0] as string,
                WithdrawalMarker,
                StringComparison.Ordinal
            ))
            {
                return false;
            }

            string apiId = fields[1] as string;

            if (string.IsNullOrWhiteSpace(apiId))
                return false;

            if (!(fields[2] is Guid))
                return false;

            Guid providerInstanceId = (Guid)fields[2];

            if (providerInstanceId == Guid.Empty)
                return false;

            withdrawal = new ApiProviderWithdrawal(
                apiId,
                providerInstanceId
            );

            return true;
        }

        private static bool TryParseLegacyAnnouncement(
            object[] fields,
            out ApiAnnouncement announcement
        )
        {
            announcement = null;

            if (fields.Length != 5)
                return false;

            return TryCreateAnnouncement(
                fields[1],
                fields[2],
                Guid.Empty,
                fields[3],
                fields[4],
                out announcement
            );
        }

        private static bool TryParseVersionTwoAnnouncement(
            object[] fields,
            out ApiAnnouncement announcement
        )
        {
            announcement = null;

            if (fields.Length != 6)
                return false;

            if (!(fields[3] is Guid))
                return false;

            Guid providerInstanceId = (Guid)fields[3];

            if (providerInstanceId == Guid.Empty)
                return false;

            return TryCreateAnnouncement(
                fields[1],
                fields[2],
                providerInstanceId,
                fields[4],
                fields[5],
                out announcement
            );
        }

        private static bool TryCreateAnnouncement(
            object apiIdField,
            object versionField,
            Guid providerInstanceId,
            object correlationField,
            object endpointsField,
            out ApiAnnouncement announcement
        )
        {
            announcement = null;

            string apiId = apiIdField as string;
            string versionText = versionField as string;

            if (string.IsNullOrWhiteSpace(apiId))
                return false;

            SemanticVersion version;

            if (!SemanticVersion.TryParse(
                versionText,
                out version
            ))
            {
                return false;
            }

            if (!(correlationField is Guid))
                return false;

            var endpoints =
                endpointsField as IDictionary<string, Delegate>;

            if (endpoints == null)
                return false;

            try
            {
                announcement = new ApiAnnouncement(
                    new ApiDescriptor(apiId, version),
                    providerInstanceId,
                    (Guid)correlationField,
                    endpoints
                );

                return true;
            }
            catch (Exception)
            {
                announcement = null;
                return false;
            }
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