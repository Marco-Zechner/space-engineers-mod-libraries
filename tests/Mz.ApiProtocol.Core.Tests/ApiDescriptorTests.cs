using System;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiDescriptorTests
    {
        [Fact]
        public void Constructor_StoresNormalizedIdAndVersion()
        {
            var version = new SemanticVersion(1, 2, 3);

            var descriptor = new ApiDescriptor(
                "  Mz.CommandAPI  ",
                version
            );

            Assert.Equal("Mz.CommandAPI", descriptor.ApiId);
            Assert.Same(version, descriptor.Version);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidApiId_ThrowsArgumentException(
            string? apiId
        )
        {
            Assert.ThrowsAny<ArgumentException>(
                delegate
                {
                    new ApiDescriptor(
                        apiId!,
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
                    new ApiDescriptor(
                        "Mz.CommandAPI",
                        null!
                    );
                }
            );
        }

        [Fact]
        public void ToString_ReturnsIdAndVersion()
        {
            var descriptor = new ApiDescriptor(
                "Mz.CommandAPI",
                new SemanticVersion(1, 2, 3)
            );

            Assert.Equal(
                "Mz.CommandAPI 1.2.3",
                descriptor.ToString()
            );
        }
    }
}