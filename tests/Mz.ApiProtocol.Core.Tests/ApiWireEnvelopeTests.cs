using System;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiWireEnvelopeTests
    {
        [Fact]
        public void TryParseEnvelope_Request_ReturnsConsumerMetadata()
        {
            Guid correlationId = Guid.NewGuid();

            object payload =
                ApiDiscoveryWireProtocol.CreateRequest(
                    CreateDependency(),
                    correlationId
                );

            Assert.True(
                ApiDiscoveryWireProtocol.TryParseEnvelope(
                    payload,
                    out ApiWireEnvelope envelope
                )
            );

            Assert.Equal(
                ApiWireMessageKind.Request,
                envelope.MessageKind
            );

            Assert.Equal(
                "Mz.ConsumerMod",
                envelope.Participant.Id
            );

            Assert.Equal(
                "Mz.CommandAPI",
                envelope.ApiId
            );

            Assert.Equal(
                ApiProtocolInfo.WireProtocolVersion,
                envelope.WireProtocolVersion
            );

            Assert.Equal(
                ApiProtocolInfo.LibraryVersion,
                envelope.LibraryVersion
            );
        }

        [Fact]
        public void TryParseEnvelope_IncompatibleMinimalHeader_Succeeds()
        {
            var payload = new object[]
            {
                ApiDiscoveryWireProtocol.AnnouncementMarker,
                "2.0.0",
                "4.3.2",
                "Mz.FutureProvider",
                "Future Provider",
                "7.0.0",
                "Mz.CommandAPI"
            };

            Assert.True(
                ApiDiscoveryWireProtocol.TryParseEnvelope(
                    payload,
                    out ApiWireEnvelope envelope
                )
            );

            Assert.Equal(
                new SemanticVersion(2, 0, 0),
                envelope.WireProtocolVersion
            );

            Assert.Equal(
                new SemanticVersion(4, 3, 2),
                envelope.LibraryVersion
            );

            Assert.Equal(
                "Mz.FutureProvider",
                envelope.Participant.Id
            );
        }

        [Fact]
        public void TryParseEnvelope_UnknownMarker_ReturnsFalse()
        {
            var payload = new object[]
            {
                "Unknown.Message",
                "1.0.0",
                "0.1.0",
                "Mz.Mod",
                "Mod",
                "1.0.0",
                "Mz.CommandAPI"
            };

            Assert.False(
                ApiDiscoveryWireProtocol.TryParseEnvelope(
                    payload,
                    out ApiWireEnvelope envelope
                )
            );

            Assert.Null(envelope);
        }

        private static ApiDependencyDescriptor CreateDependency()
        {
            return new ApiDependencyDescriptor(
                new ApiModIdentity(
                    "Mz.ConsumerMod",
                    "Consumer Mod",
                    new SemanticVersion(2, 0, 0)
                ),
                new ApiRequirement(
                    "Mz.CommandAPI",
                    new ApiVersionRange(
                        new SemanticVersion(1, 0, 0),
                        new SemanticVersion(2, 0, 0)
                    )
                ),
                ApiDependencyKind.Optional,
                "Adds Command API integration"
            );
        }
    }
}