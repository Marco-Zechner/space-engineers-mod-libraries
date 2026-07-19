using System;
using System.Collections.Generic;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiDiscoveryWireProtocolTests
    {
        [Fact]
        public void CreateRequest_UsesVersionedTransportSafeFields()
        {
            Guid correlationId = Guid.NewGuid();

            object payload =
                ApiDiscoveryWireProtocol.CreateRequest(
                    "  Mz.CommandAPI  ",
                    correlationId
                );

            object[] fields = Assert.IsType<object[]>(payload);

            Assert.Equal(5, fields.Length);

            Assert.Equal(
                ApiDiscoveryWireProtocol.RequestMarker,
                fields[0]
            );

            Assert.Equal(
                ApiProtocolInfo.WireProtocolVersion.ToString(),
                fields[1]
            );

            Assert.Equal(
                ApiProtocolInfo.LibraryVersion.ToString(),
                fields[2]
            );

            Assert.Equal("Mz.CommandAPI", fields[3]);
            Assert.Equal(correlationId, fields[4]);
        }

        [Fact]
        public void TryParseRequest_ValidPayload_ReturnsRequest()
        {
            Guid correlationId = Guid.NewGuid();

            object payload =
                ApiDiscoveryWireProtocol.CreateRequest(
                    "Mz.CommandAPI",
                    correlationId
                );

            bool success =
                ApiDiscoveryWireProtocol.TryParseRequest(
                    payload,
                    out ApiDiscoveryRequest request
                );

            Assert.True(success);
            Assert.NotNull(request);

            Assert.Equal("Mz.CommandAPI", request.ApiId);
            Assert.Equal(correlationId, request.CorrelationId);

            Assert.Equal(
                ApiProtocolInfo.WireProtocolVersion,
                request.WireProtocolVersion
            );

            Assert.Equal(
                ApiProtocolInfo.LibraryVersion,
                request.LibraryVersion
            );
        }

        [Fact]
        public void TryParseRequest_AdditionalFields_AreIgnored()
        {
            Guid correlationId = Guid.NewGuid();

            var payload = new object[]
            {
                ApiDiscoveryWireProtocol.RequestMarker,
                ApiProtocolInfo.WireProtocolVersion.ToString(),
                ApiProtocolInfo.LibraryVersion.ToString(),
                "Mz.CommandAPI",
                correlationId,
                "future optional field"
            };

            bool success =
                ApiDiscoveryWireProtocol.TryParseRequest(
                    payload,
                    out ApiDiscoveryRequest request
                );

            Assert.True(success);
            Assert.Equal(correlationId, request.CorrelationId);
        }

        [Theory]
        [MemberData(nameof(GetInvalidRequestPayloads))]
        public void TryParseRequest_InvalidPayload_ReturnsFalse(
            object? payload
        )
        {
            bool success =
                ApiDiscoveryWireProtocol.TryParseRequest(
                    payload!,
                    out ApiDiscoveryRequest request
                );

            Assert.False(success);
            Assert.Null(request);
        }

        [Fact]
        public void CreateAnnouncement_UsesVersionedTransportSafeFields()
        {
            Guid providerInstanceId = Guid.NewGuid();
            Guid correlationId = Guid.NewGuid();

            Action endpoint =
                delegate
                {
                };

            object payload =
                ApiDiscoveryWireProtocol.CreateAnnouncement(
                    CreateDescriptor(),
                    providerInstanceId,
                    correlationId,
                    new Dictionary<string, Delegate>
                    {
                        { "RegisterCommand", endpoint }
                    }
                );

            object[] fields = Assert.IsType<object[]>(payload);

            Assert.Equal(8, fields.Length);

            Assert.Equal(
                ApiDiscoveryWireProtocol.AnnouncementMarker,
                fields[0]
            );

            Assert.Equal(
                ApiProtocolInfo.WireProtocolVersion.ToString(),
                fields[1]
            );

            Assert.Equal(
                ApiProtocolInfo.LibraryVersion.ToString(),
                fields[2]
            );

            Assert.Equal("Mz.CommandAPI", fields[3]);
            Assert.Equal("1.2.3", fields[4]);
            Assert.Equal(providerInstanceId, fields[5]);
            Assert.Equal(correlationId, fields[6]);

            var endpoints =
                Assert.IsType<Dictionary<string, Delegate>>(
                    fields[7]
                );

            Assert.Same(
                endpoint,
                endpoints["RegisterCommand"]
            );
        }

        [Fact]
        public void TryParseAnnouncement_ValidPayload_ReturnsAnnouncement()
        {
            Guid providerInstanceId = Guid.NewGuid();
            Guid correlationId = Guid.NewGuid();

            Action endpoint =
                delegate
                {
                };

            object payload =
                ApiDiscoveryWireProtocol.CreateAnnouncement(
                    CreateDescriptor(),
                    providerInstanceId,
                    correlationId,
                    new Dictionary<string, Delegate>
                    {
                        { "RegisterCommand", endpoint }
                    }
                );

            bool success =
                ApiDiscoveryWireProtocol.TryParseAnnouncement(
                    payload,
                    out ApiAnnouncement announcement
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
                providerInstanceId,
                announcement.ProviderInstanceId
            );

            Assert.Equal(
                correlationId,
                announcement.CorrelationId
            );

            Assert.Equal(
                ApiProtocolInfo.WireProtocolVersion,
                announcement.WireProtocolVersion
            );

            Assert.Equal(
                ApiProtocolInfo.LibraryVersion,
                announcement.LibraryVersion
            );

            Assert.Same(
                endpoint,
                announcement.Endpoints["RegisterCommand"]
            );
        }

        [Fact]
        public void TryParseAnnouncement_AllowsUnsolicitedAnnouncement()
        {
            object payload =
                ApiDiscoveryWireProtocol.CreateAnnouncement(
                    CreateDescriptor(),
                    Guid.NewGuid(),
                    Guid.Empty,
                    new Dictionary<string, Delegate>()
                );

            bool success =
                ApiDiscoveryWireProtocol.TryParseAnnouncement(
                    payload,
                    out ApiAnnouncement announcement
                );

            Assert.True(success);

            Assert.Equal(
                Guid.Empty,
                announcement.CorrelationId
            );
        }

        [Fact]
        public void TryParseAnnouncement_AdditionalFields_AreIgnored()
        {
            Guid providerInstanceId = Guid.NewGuid();

            var payload = new object[]
            {
                ApiDiscoveryWireProtocol.AnnouncementMarker,
                ApiProtocolInfo.WireProtocolVersion.ToString(),
                ApiProtocolInfo.LibraryVersion.ToString(),
                "Mz.CommandAPI",
                "1.2.3",
                providerInstanceId,
                Guid.Empty,
                new Dictionary<string, Delegate>(),
                "future optional field"
            };

            bool success =
                ApiDiscoveryWireProtocol.TryParseAnnouncement(
                    payload,
                    out ApiAnnouncement announcement
                );

            Assert.True(success);

            Assert.Equal(
                providerInstanceId,
                announcement.ProviderInstanceId
            );
        }

        [Theory]
        [MemberData(nameof(GetInvalidAnnouncementPayloads))]
        public void TryParseAnnouncement_InvalidPayload_ReturnsFalse(
            object? payload
        )
        {
            bool success =
                ApiDiscoveryWireProtocol.TryParseAnnouncement(
                    payload!,
                    out ApiAnnouncement announcement
                );

            Assert.False(success);
            Assert.Null(announcement);
        }

        [Fact]
        public void CreateWithdrawal_UsesVersionedTransportSafeFields()
        {
            Guid providerInstanceId = Guid.NewGuid();

            object payload =
                ApiDiscoveryWireProtocol.CreateWithdrawal(
                    "  Mz.CommandAPI  ",
                    providerInstanceId
                );

            object[] fields = Assert.IsType<object[]>(payload);

            Assert.Equal(5, fields.Length);

            Assert.Equal(
                ApiDiscoveryWireProtocol.WithdrawalMarker,
                fields[0]
            );

            Assert.Equal(
                ApiProtocolInfo.WireProtocolVersion.ToString(),
                fields[1]
            );

            Assert.Equal(
                ApiProtocolInfo.LibraryVersion.ToString(),
                fields[2]
            );

            Assert.Equal("Mz.CommandAPI", fields[3]);
            Assert.Equal(providerInstanceId, fields[4]);
        }

        [Fact]
        public void TryParseWithdrawal_ValidPayload_ReturnsWithdrawal()
        {
            Guid providerInstanceId = Guid.NewGuid();

            object payload =
                ApiDiscoveryWireProtocol.CreateWithdrawal(
                    "Mz.CommandAPI",
                    providerInstanceId
                );

            bool success =
                ApiDiscoveryWireProtocol.TryParseWithdrawal(
                    payload,
                    out ApiProviderWithdrawal withdrawal
                );

            Assert.True(success);
            Assert.NotNull(withdrawal);

            Assert.Equal(
                "Mz.CommandAPI",
                withdrawal.ApiId
            );

            Assert.Equal(
                providerInstanceId,
                withdrawal.ProviderInstanceId
            );

            Assert.Equal(
                ApiProtocolInfo.WireProtocolVersion,
                withdrawal.WireProtocolVersion
            );

            Assert.Equal(
                ApiProtocolInfo.LibraryVersion,
                withdrawal.LibraryVersion
            );
        }

        [Fact]
        public void TryParseWithdrawal_AdditionalFields_AreIgnored()
        {
            Guid providerInstanceId = Guid.NewGuid();

            var payload = new object[]
            {
                ApiDiscoveryWireProtocol.WithdrawalMarker,
                ApiProtocolInfo.WireProtocolVersion.ToString(),
                ApiProtocolInfo.LibraryVersion.ToString(),
                "Mz.CommandAPI",
                providerInstanceId,
                "future optional field"
            };

            bool success =
                ApiDiscoveryWireProtocol.TryParseWithdrawal(
                    payload,
                    out ApiProviderWithdrawal withdrawal
                );

            Assert.True(success);

            Assert.Equal(
                providerInstanceId,
                withdrawal.ProviderInstanceId
            );
        }

        [Theory]
        [MemberData(nameof(GetInvalidWithdrawalPayloads))]
        public void TryParseWithdrawal_InvalidPayload_ReturnsFalse(
            object? payload
        )
        {
            bool success =
                ApiDiscoveryWireProtocol.TryParseWithdrawal(
                    payload!,
                    out ApiProviderWithdrawal withdrawal
                );

            Assert.False(success);
            Assert.Null(withdrawal);
        }

        public static IEnumerable<object?[]>
            GetInvalidRequestPayloads()
        {
            yield return [null];
            yield return ["not an array"];
            yield return [Array.Empty<object>()];

            yield return
            [
                new object[]
                {
                    "wrong marker",
                    "1.0.0",
                    "0.1.0",
                    "Mz.CommandAPI",
                    Guid.NewGuid()
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.RequestMarker,
                    "invalid wire version",
                    "0.1.0",
                    "Mz.CommandAPI",
                    Guid.NewGuid()
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.RequestMarker,
                    "1.0.0",
                    "invalid library version",
                    "Mz.CommandAPI",
                    Guid.NewGuid()
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.RequestMarker,
                    "1.0.0",
                    "0.1.0",
                    "",
                    Guid.NewGuid()
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.RequestMarker,
                    "1.0.0",
                    "0.1.0",
                    "Mz.CommandAPI",
                    Guid.Empty
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.RequestMarker,
                    "1.0.0",
                    "0.1.0",
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
                CreateAnnouncementFields(
                    marker: "wrong marker"
                )
            ];

            yield return
            [
                CreateAnnouncementFields(
                    wireVersion: "invalid"
                )
            ];

            yield return
            [
                CreateAnnouncementFields(
                    libraryVersion: "invalid"
                )
            ];

            yield return
            [
                CreateAnnouncementFields(
                    apiId: ""
                )
            ];

            yield return
            [
                CreateAnnouncementFields(
                    apiVersion: "invalid"
                )
            ];

            yield return
            [
                CreateAnnouncementFields(
                    providerInstanceId: Guid.Empty
                )
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.AnnouncementMarker,
                    "1.0.0",
                    "0.1.0",
                    "Mz.CommandAPI",
                    "1.2.3",
                    "not a guid",
                    Guid.Empty,
                    new Dictionary<string, Delegate>()
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.AnnouncementMarker,
                    "1.0.0",
                    "0.1.0",
                    "Mz.CommandAPI",
                    "1.2.3",
                    Guid.NewGuid(),
                    "not a guid",
                    new Dictionary<string, Delegate>()
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.AnnouncementMarker,
                    "1.0.0",
                    "0.1.0",
                    "Mz.CommandAPI",
                    "1.2.3",
                    Guid.NewGuid(),
                    Guid.Empty,
                    "not an endpoint dictionary"
                }
            ];

            yield return
            [
                CreateAnnouncementFields(
                    endpoints:
                        new Dictionary<string, Delegate>
                        {
                            { "Invalid", null! }
                        }
                )
            ];
        }

        public static IEnumerable<object?[]>
            GetInvalidWithdrawalPayloads()
        {
            yield return [null];
            yield return ["not an array"];
            yield return [Array.Empty<object>()];

            yield return
            [
                new object[]
                {
                    "wrong marker",
                    "1.0.0",
                    "0.1.0",
                    "Mz.CommandAPI",
                    Guid.NewGuid()
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.WithdrawalMarker,
                    "invalid",
                    "0.1.0",
                    "Mz.CommandAPI",
                    Guid.NewGuid()
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.WithdrawalMarker,
                    "1.0.0",
                    "invalid",
                    "Mz.CommandAPI",
                    Guid.NewGuid()
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.WithdrawalMarker,
                    "1.0.0",
                    "0.1.0",
                    "",
                    Guid.NewGuid()
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.WithdrawalMarker,
                    "1.0.0",
                    "0.1.0",
                    "Mz.CommandAPI",
                    Guid.Empty
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.WithdrawalMarker,
                    "1.0.0",
                    "0.1.0",
                    "Mz.CommandAPI",
                    "not a guid"
                }
            ];
        }

        private static object[] CreateAnnouncementFields(
            string marker =
                ApiDiscoveryWireProtocol.AnnouncementMarker,
            string wireVersion = "1.0.0",
            string libraryVersion = "0.1.0",
            string apiId = "Mz.CommandAPI",
            string apiVersion = "1.2.3",
            Guid? providerInstanceId = null,
            Guid? correlationId = null,
            object? endpoints = null
        )
        {
            return
            [
                marker,
                wireVersion,
                libraryVersion,
                apiId,
                apiVersion,
                providerInstanceId ?? Guid.NewGuid(),
                correlationId ?? Guid.Empty,
                endpoints
                    ?? new Dictionary<string, Delegate>()
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