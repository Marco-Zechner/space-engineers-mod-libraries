using System;
using Xunit;

namespace Mz.SemanticVersioning.Tests
{
    public sealed class SemanticVersionParsingTests
    {
        [Fact]
        public void Parse_ValidVersion_ReturnsComponents()
        {
            var version = SemanticVersion.Parse("1.2.3");

            Assert.Equal(1, version.Major);
            Assert.Equal(2, version.Minor);
            Assert.Equal(3, version.Patch);
        }

        [Fact]
        public void Parse_SurroundingWhitespace_TrimsInput()
        {
            var version = SemanticVersion.Parse("  \t1.2.3\r\n");

            Assert.Equal(1, version.Major);
            Assert.Equal(2, version.Minor);
            Assert.Equal(3, version.Patch);
        }

        [Fact]
        public void Parse_LeadingZeroes_NormalizesComponents()
        {
            var version = SemanticVersion.Parse("01.002.0003");

            Assert.Equal(1, version.Major);
            Assert.Equal(2, version.Minor);
            Assert.Equal(3, version.Patch);
            Assert.Equal("1.2.3", version.ToString());
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("1")]
        [InlineData("1.2")]
        [InlineData("1.2.3.4")]
        [InlineData(".1.2")]
        [InlineData("1..2")]
        [InlineData("1.2.")]
        [InlineData("-1.2.3")]
        [InlineData("+1.2.3")]
        [InlineData("1.-2.3")]
        [InlineData("1.2.-3")]
        [InlineData("1.nope.3")]
        [InlineData("1. 2.3")]
        [InlineData("1.2 .3")]
        [InlineData("1.2. 3")]
        [InlineData("1.2.2147483648")]
        public void Parse_InvalidVersion_ThrowsFormatException(string input)
        {
            Assert.Throws<FormatException>(
                delegate
                {
                    SemanticVersion.Parse(input);
                }
            );
        }

        [Fact]
        public void Parse_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    SemanticVersion.Parse(null);
                }
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("1")]
        [InlineData("1.2")]
        [InlineData("1.2.3.4")]
        [InlineData("-1.2.3")]
        [InlineData("1.nope.3")]
        [InlineData("1. 2.3")]
        [InlineData("2147483648.0.0")]
        public void TryParse_InvalidVersion_ReturnsFalse(string? input)
        {
            var success = SemanticVersion.TryParse(input, out var version);

            Assert.False(success);
            Assert.Null(version);
        }

        [Fact]
        public void TryParse_ValidVersion_ReturnsTrueAndVersion()
        {
            var success = SemanticVersion.TryParse(
                "  10.20.30  ",
                out var version
            );

            Assert.True(success);
            Assert.NotNull(version);
            Assert.Equal(10, version.Major);
            Assert.Equal(20, version.Minor);
            Assert.Equal(30, version.Patch);
        }
    }
}