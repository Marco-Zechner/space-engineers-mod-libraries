using System;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiProtocolInfoTests
    {
        [Fact]
        public void CurrentVersions_AreDefined()
        {
            Assert.Equal(
                new SemanticVersion(0, 2, 2),
                ApiProtocolInfo.LibraryVersion
            );

            Assert.Equal(
                new SemanticVersion(1, 0, 0),
                ApiProtocolInfo.WireProtocolVersion
            );
        }

        [Theory]
        [InlineData(1, 0, 0)]
        [InlineData(1, 5, 0)]
        [InlineData(1, 999, 999)]
        public void EvaluateWireProtocol_SameMajor_ReturnsCompatible(
            int major,
            int minor,
            int patch
        )
        {
            Assert.Equal(
                ApiWireCompatibilityStatus.Compatible,
                ApiProtocolInfo.EvaluateWireProtocol(
                    new SemanticVersion(
                        major,
                        minor,
                        patch
                    )
                )
            );
        }

        [Fact]
        public void EvaluateWireProtocol_OlderMajor_ReturnsRemoteTooOld()
        {
            Assert.Equal(
                ApiWireCompatibilityStatus.RemoteTooOld,
                ApiProtocolInfo.EvaluateWireProtocol(
                    new SemanticVersion(0, 99, 0)
                )
            );
        }

        [Fact]
        public void EvaluateWireProtocol_NewerMajor_ReturnsRemoteTooNew()
        {
            Assert.Equal(
                ApiWireCompatibilityStatus.RemoteTooNew,
                ApiProtocolInfo.EvaluateWireProtocol(
                    new SemanticVersion(2, 0, 0)
                )
            );
        }

        [Fact]
        public void EvaluateWireProtocol_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    ApiProtocolInfo.EvaluateWireProtocol(null!);
                }
            );
        }
    }
}
