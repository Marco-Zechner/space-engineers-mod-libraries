using System;
using System.Collections.Generic;
using Mz.SemanticVersioning;

namespace Mz.ApiProtocol
{
    /// <summary>
    /// Encodes and decodes API discovery messages using only runtime types
    /// shared safely between separately compiled mods.
    /// </summary>
    public static class ApiDiscoveryWireProtocol
    {
        /// <summary>
        /// Marks a discovery request payload.
        /// </summary>
        public const string RequestMarker =
            "Mz.ApiProtocol.Discovery.Request/1";

        /// <summary>
        /// Marks a provider announcement payload.
        /// </summary>
        public const string AnnouncementMarker =
            "Mz.ApiProtocol.Discovery.Announcement/1";

        /// <summary>
        /// Creates a transport-safe discovery request payload.
        /// </summary>
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
        public static bool TryParseRequest(
            object payload,
            out ApiDiscoveryRequest request
        )
        {
            request = null;

            var fields = payload as object[];

            if (fields == null || fields.Length != 3)
                return false;

            var marker = fields[0] as string;
            var apiId = fields[1] as string;

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

            var correlationId = (Guid)fields[2];

            if (correlationId == Guid.Empty)
                return false;

            request = new ApiDiscoveryRequest(
                apiId,
                correlationId
            );

            return true;
        }

        /// <summary>
        /// Creates a transport-safe provider announcement payload.
        /// </summary>
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

            var endpointPayload =
                new Dictionary<string, Delegate>(
                    StringComparer.Ordinal
                );

            foreach (
                var pair
                in announcement.Endpoints
            )
            {
                endpointPayload.Add(
                    pair.Key,
                    pair.Value
                );
            }

            return new object[]
            {
                AnnouncementMarker,
                announcement.Descriptor.ApiId,
                announcement.Descriptor.Version.ToString(),
                announcement.CorrelationId,
                endpointPayload
            };
        }

        /// <summary>
        /// Attempts to decode a provider announcement payload.
        /// </summary>
        public static bool TryParseAnnouncement(
            object payload,
            out ApiAnnouncement announcement
        )
        {
            announcement = null;

            var fields = payload as object[];

            if (fields == null || fields.Length != 5)
                return false;

            var marker = fields[0] as string;
            var apiId = fields[1] as string;
            var versionText = fields[2] as string;

            if (!string.Equals(
                marker,
                AnnouncementMarker,
                StringComparison.Ordinal
            ))
            {
                return false;
            }

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

            if (!(fields[3] is Guid))
                return false;

            var endpoints =
                fields[4] as IDictionary<string, Delegate>;

            if (endpoints == null)
                return false;

            try
            {
                announcement = new ApiAnnouncement(
                    new ApiDescriptor(apiId, version),
                    (Guid)fields[3],
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
    }
}