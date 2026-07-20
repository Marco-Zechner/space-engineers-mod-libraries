using System;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiModIdentityTests
    {
        [Fact]
        public void Constructor_NormalizesAndStoresValues()
        {
            var version = new SemanticVersion(1, 2, 3);

            var identity = new ApiModIdentity(
                "  Mz.CommandApiMod  ",
                "  Command API  ",
                version
            );

            Assert.Equal(
                "Mz.CommandApiMod",
                identity.Id
            );

            Assert.Equal(
                "Command API",
                identity.DisplayName
            );

            Assert.Same(version, identity.Version);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidId_ThrowsArgumentException(
            string? id
        )
        {
            Assert.ThrowsAny<ArgumentException>(
                delegate
                {
                    new ApiModIdentity(
                        id!,
                        "Command API",
                        new SemanticVersion(1, 0, 0)
                    );
                }
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidDisplayName_ThrowsArgumentException(
            string? displayName
        )
        {
            Assert.ThrowsAny<ArgumentException>(
                delegate
                {
                    new ApiModIdentity(
                        "Mz.CommandApiMod",
                        displayName!,
                        new SemanticVersion(1, 0, 0)
                    );
                }
            );
        }

        [Fact]
        public void Constructor_NullVersion_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    new ApiModIdentity(
                        "Mz.CommandApiMod",
                        "Command API",
                        null!
                    );
                }
            );
        }

        [Fact]
        public void ToString_IncludesDisplayNameIdAndVersion()
        {
            var identity = new ApiModIdentity(
                "Mz.CommandApiMod",
                "Command API",
                new SemanticVersion(1, 2, 3)
            );

            Assert.Equal(
                "Command API (Mz.CommandApiMod) 1.2.3",
                identity.ToString()
            );
        }
    }
}