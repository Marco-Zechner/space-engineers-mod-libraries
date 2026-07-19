using System;
using System.Collections.Generic;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiDiscoveryWireProtocolV2Tests
    {
        [Fact]
        public void VersionTwoAnnouncement_PreservesProviderIdentity()
        {
            Guid providerInstanceId = Guid.NewGuid();

            object payload =
                ApiDiscoveryWireProtocol.CreateAnnouncement(
                    CreateDescriptor(),
                    providerInstanceId,
                    Guid.Empty,
                    CreateEndpoints()
                );

            object[] fields = Assert.IsType<object[]>(payload);

            Assert.Equal(
                ApiDiscoveryWireProtocol.AnnouncementMarkerV2,
                fields[0]
            );

            Assert.Equal(6, fields.Length);

            Assert.True(
                ApiDiscoveryWireProtocol.TryParseAnnouncement(
                    payload,
                    out ApiAnnouncement announcement
                )
            );

            Assert.Equal(
                providerInstanceId,
                announcement.ProviderInstanceId
            );
        }

        [Fact]
        public void LegacyAnnouncement_UsesEmptyProviderIdentity()
        {
            object payload =
                ApiDiscoveryWireProtocol.CreateAnnouncement(
                    CreateDescriptor(),
                    Guid.Empty,
                    CreateEndpoints()
                );

            Assert.True(
                ApiDiscoveryWireProtocol.TryParseAnnouncement(
                    payload,
                    out ApiAnnouncement announcement
                )
            );

            Assert.Equal(
                Guid.Empty,
                announcement.ProviderInstanceId
            );
        }

        [Fact]
        public void VersionTwoAnnouncement_EmptyProviderId_Throws()
        {
            Assert.Throws<ArgumentException>(
                delegate
                {
                    ApiDiscoveryWireProtocol.CreateAnnouncement(
                        CreateDescriptor(),
                        Guid.Empty,
                        Guid.Empty,
                        CreateEndpoints()
                    );
                }
            );
        }

        [Fact]
        public void Withdrawal_RoundTrips()
        {
            Guid providerInstanceId = Guid.NewGuid();

            object payload =
                ApiDiscoveryWireProtocol.CreateWithdrawal(
                    "Mz.CommandAPI",
                    providerInstanceId
                );

            Assert.True(
                ApiDiscoveryWireProtocol.TryParseWithdrawal(
                    payload,
                    out ApiProviderWithdrawal withdrawal
                )
            );

            Assert.Equal(
                "Mz.CommandAPI",
                withdrawal.ApiId
            );

            Assert.Equal(
                providerInstanceId,
                withdrawal.ProviderInstanceId
            );
        }

        [Theory]
        [MemberData(nameof(GetInvalidWithdrawals))]
        public void TryParseWithdrawal_InvalidPayload_ReturnsFalse(
            object? payload
        )
        {
            Assert.False(
                ApiDiscoveryWireProtocol.TryParseWithdrawal(
                    payload!,
                    out ApiProviderWithdrawal withdrawal
                )
            );

            Assert.Null(withdrawal);
        }

        public static IEnumerable<object?[]>
            GetInvalidWithdrawals()
        {
            yield return [null];
            yield return ["invalid"];
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
                    ApiDiscoveryWireProtocol.WithdrawalMarker,
                    "",
                    Guid.NewGuid()
                }
            ];

            yield return
            [
                new object[]
                {
                    ApiDiscoveryWireProtocol.WithdrawalMarker,
                    "Mz.CommandAPI",
                    Guid.Empty
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

        private static IDictionary<string, Delegate>
            CreateEndpoints()
        {
            return new Dictionary<string, Delegate>
            {
                {
                    "Ping",
                    (Action)delegate
                    {
                    }
                }
            };
        }
    }
}