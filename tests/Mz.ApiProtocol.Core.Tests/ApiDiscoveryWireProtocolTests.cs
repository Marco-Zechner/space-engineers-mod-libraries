using System;
using System.Collections.Generic;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiDiscoveryWireProtocolTests
    {
        [Fact]
        public void CreateRequest_UsesOnlyTransportSafeFields()
        {
            var correlationId = Guid.NewGuid();

            var payload =
                ApiDiscoveryWireProtocol.CreateRequest(
                    "  Mz.CommandAPI  ",
                    correlationId
                );

            var fields = Assert.IsType<object[]>(payload);

            Assert.Equal(3, fields.Length);

            Assert.Equal(
                ApiDiscoveryWireProtocol.RequestMarker,
                fields[0]
            );

            Assert.Equal("Mz.CommandAPI", fields[1]);
            Assert.Equal(correlationId, fields[2]);
        }

        [Fact]
        public void TryParseRequest_ValidPayload_ReturnsRequest()
        {
            var correlationId = Guid.NewGuid();

            var payload =
                ApiDiscoveryWireProtocol.CreateRequest(
                    "Mz.CommandAPI",
                    correlationId
                );

            var success =
                ApiDiscoveryWireProtocol.TryParseRequest(
                    payload,
                    out var request
                );

            Assert.True(success);
            Assert.NotNull(request);
            Assert.Equal("Mz.CommandAPI", request.ApiId);
            Assert.Equal(correlationId, request.CorrelationId);
        }

        [Theory]
        [MemberData(nameof(GetInvalidRequestPayloads))]
        public void TryParseRequest_InvalidPayload_ReturnsFalse(
            object? payload
        )
        {
            var success =
                ApiDiscoveryWireProtocol.TryParseRequest(
                    payload!,
                    out var request
                );

            Assert.False(success);
            Assert.Null(request);
        }

        [Fact]
        public void CreateAnnouncement_UsesTransportSafeFields()
        {
            var correlationId = Guid.NewGuid();

            Action endpoint =
                delegate
                {
                };

            var payload =
                ApiDiscoveryWireProtocol.CreateAnnouncement(
                    CreateDescriptor(),
                    correlationId,
                    new Dictionary<string, Delegate>
                    {
                        { "RegisterCommand", endpoint }
                    }
                );

            var fields = Assert.IsType<object[]>(payload);

            Assert.Equal(5, fields.Length);

            Assert.Equal(
                ApiDiscoveryWireProtocol.AnnouncementMarker,
                fields[0]
            );

            Assert.Equal("Mz.CommandAPI", fields[1]);
            Assert.Equal("1.2.3", fields[2]);
            Assert.Equal(correlationId, fields[3]);

            var endpoints =
                Assert.IsType<Dictionary<string, Delegate>>(
                    fields[4]
                );

            Assert.Same(
                endpoint,
                endpoints["RegisterCommand"]
            );
        }

        [Fact]
        public void TryParseAnnouncement_ValidPayload_ReturnsAnnouncement()
        {
            var correlationId = Guid.NewGuid();

            Action endpoint =
                delegate
                {
                };

            var payload =
                ApiDiscoveryWireProtocol.CreateAnnouncement(
                    CreateDescriptor(),
                    correlationId,
                    new Dictionary<string, Delegate>
                    {
                        { "RegisterCommand", endpoint }
                    }
                );

            var success =
                ApiDiscoveryWireProtocol.TryParseAnnouncement(
                    payload,
                    out var announcement
                );

            Assert.True(success);
            Assert.NotNull(announcement);

            Assert.Equal(
                "Mz.CommandAPI",
                announcement.Descriptor.ApiId
            );

            Assert.Equal(
                new SemanticVersion(1, 2, 3),
                announcement.Descriptor.Version
            );

            Assert.Equal(
                correlationId,
                announcement.CorrelationId
            );

            Assert.Same(
                endpoint,
                announcement.Endpoints["RegisterCommand"]
            );
        }

        [Fact]
        public void TryParseAnnouncement_AllowsUnsolicitedAnnouncement()
        {
            var payload =
                ApiDiscoveryWireProtocol.CreateAnnouncement(
                    CreateDescriptor(),
                    Guid.Empty,
                    new Dictionary<string, Delegate>()
                );

            var success =
                ApiDiscoveryWireProtocol.TryParseAnnouncement(
                    payload,
                    out var announcement
                );

            Assert.True(success);
            Assert.Equal(
                Guid.Empty,
                announcement.CorrelationId
            );
        }

        [Theory]
        [MemberData(nameof(GetInvalidAnnouncementPayloads))]
        public void TryParseAnnouncement_InvalidPayload_ReturnsFalse(
            object? payload
        )
        {
            var success =
                ApiDiscoveryWireProtocol.TryParseAnnouncement(
                    payload!,
                    out var announcement
                );

            Assert.False(success);
            Assert.Null(announcement);
        }

        public static IEnumerable<object?[]> GetInvalidRequestPayloads()
        {
            yield return [null];
            yield return ["not an array"];
            yield return [Array.Empty<object>()];

            yield return
            [
                new object[]
                {
                    "wrong marker",
                    "Mz.CommandAPI",
                    Guid.NewGuid()
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.RequestMarker,
                    "",
                    Guid.NewGuid()
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.RequestMarker,
                    "Mz.CommandAPI",
                    Guid.Empty
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.RequestMarker,
                    "Mz.CommandAPI",
                    "not a guid"
                }
            ];
        }

        public static IEnumerable<object?[]>
            GetInvalidAnnouncementPayloads()
        {
            yield return [null];
            yield return ["not an array"];
            yield return [Array.Empty<object>()];

            yield return
            [
                new object[]
                {
                    "wrong marker",
                    "Mz.CommandAPI",
                    "1.2.3",
                    Guid.Empty,
                    new Dictionary<string, Delegate>()
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.AnnouncementMarker,
                    "",
                    "1.2.3",
                    Guid.Empty,
                    new Dictionary<string, Delegate>()
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.AnnouncementMarker,
                    "Mz.CommandAPI",
                    "invalid",
                    Guid.Empty,
                    new Dictionary<string, Delegate>()
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.AnnouncementMarker,
                    "Mz.CommandAPI",
                    "1.2.3",
                    "not a guid",
                    new Dictionary<string, Delegate>()
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.AnnouncementMarker,
                    "Mz.CommandAPI",
                    "1.2.3",
                    Guid.Empty,
                    "not an endpoint dictionary"
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.AnnouncementMarker,
                    "Mz.CommandAPI",
                    "1.2.3",
                    Guid.Empty,
                    new Dictionary<string, Delegate>
                    {
                        { "Invalid", null! }
                    }
                }
            ];
        }

        private static ApiDescriptor CreateDescriptor()
        {
            return new ApiDescriptor(
                "Mz.CommandAPI",
                new SemanticVersion(1, 2, 3)
            );
        }
    }
}