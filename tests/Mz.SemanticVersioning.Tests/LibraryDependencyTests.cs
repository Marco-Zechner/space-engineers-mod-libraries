using System;
using Xunit;

namespace Mz.SemanticVersioning.Tests
{
    public sealed class LibraryDependencyTests
    {
        [Fact]
        public void Constructor_ValidDependency_StoresPackageAndParsedVersion()
        {
            var dependency = new LibraryDependency("Mz.ApiProtocol", "0.2.5");

            Assert.Equal("Mz.ApiProtocol", dependency.PackageId);
            Assert.Equal(new SemanticVersion(0, 2, 5), dependency.Version);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Mz ApiProtocol")]
        [InlineData(".Mz.ApiProtocol")]
        [InlineData("1Mz.ApiProtocol")]
        [InlineData("Mz-ApiProtocol")]
        [InlineData("Mz.")]
        [InlineData("Mz..ApiProtocol")]
        [InlineData("Mz.1ApiProtocol")]
        public void Constructor_InvalidPackageId_ThrowsArgumentException(string packageId)
        {
            var exception = Assert.Throws<ArgumentException>(() => new LibraryDependency(packageId, "1.2.3"));

            Assert.Equal("packageId", exception.ParamName);
        }

        [Fact]
        public void Constructor_NullPackageId_ThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new LibraryDependency(null, "1.2.3"));

            Assert.Equal("packageId", exception.ParamName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("1.2")]
        [InlineData(" 1.2.3")]
        [InlineData("1.2.3 ")]
        [InlineData("1.2.x")]
        [InlineData("2147483648.0.0")]
        public void Constructor_InvalidVersion_ThrowsFormatException(string version)
            => Assert.Throws<FormatException>(() => new LibraryDependency("Mz.Dependency", version));

        [Fact]
        public void Constructor_NullVersion_ThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new LibraryDependency("Mz.Dependency", null));

            Assert.Equal("version", exception.ParamName);
        }
    }
}
