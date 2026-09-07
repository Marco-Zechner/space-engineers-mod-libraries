using System;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiVersionRangeTests
    {
        [Fact]
        public void Constructor_StoresBounds()
        {
            var minimum = new SemanticVersion(1, 2, 0);
            var maximum = new SemanticVersion(2, 0, 0);

            var range = new ApiVersionRange(minimum, maximum);

            Assert.Same(minimum, range.MinimumInclusive);
            Assert.Same(maximum, range.MaximumExclusive);
        }

        [Fact]
        public void Constructor_AllowsMissingMaximum()
        {
            var minimum = new SemanticVersion(1, 0, 0);

            var range = new ApiVersionRange(minimum, null);

            Assert.Same(minimum, range.MinimumInclusive);
            Assert.Null(range.MaximumExclusive);
        }

        [Fact]
        public void Constructor_NullMinimum_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                {
                    new ApiVersionRange(null!, new(2, 0, 0));
                }
            );
        }

        [Theory]
        [InlineData(1, 2, 0)]
        [InlineData(1, 1, 9)]
        public void Constructor_MaximumNotGreaterThanMinimum_Throws(int major, int minor, int patch)
        {
            var minimum = new SemanticVersion(1, 2, 0);
            var maximum = new SemanticVersion(major, minor, patch);

            Assert.Throws<ArgumentException>(() =>
                {
                    new ApiVersionRange(minimum, maximum);
                }
            );
        }

        [Theory]
        [InlineData(1, 2, 0)]
        [InlineData(1, 2, 1)]
        [InlineData(1, 9, 9)]
        public void Contains_VersionInsideRange_ReturnsTrue(int major, int minor, int patch)
        {
            ApiVersionRange range = CreateRange();

            Assert.True(range.Contains(new(major, minor, patch)));
        }

        [Theory]
        [InlineData(1, 1, 9)]
        [InlineData(0, 9, 9)]
        [InlineData(2, 0, 0)]
        [InlineData(2, 1, 0)]
        public void Contains_VersionOutsideRange_ReturnsFalse(int major, int minor, int patch)
        {
            ApiVersionRange range = CreateRange();

            Assert.False(range.Contains(new(major, minor, patch)));
        }

        [Fact]
        public void Contains_UnboundedMaximumAcceptsHigherVersion()
        {
            var range = new ApiVersionRange(new(1, 0, 0), null);

            Assert.True(range.Contains(new(100, 50, 25)));
        }

        [Fact]
        public void Contains_NullVersion_ThrowsArgumentNullException()
        {
            ApiVersionRange range = CreateRange();

            Assert.Throws<ArgumentNullException>(() => range.Contains(null!));
        }

        [Fact]
        public void Evaluate_LowerVersion_ReturnsProviderTooOld()
        {
            ApiVersionRange range = CreateRange();

            Assert.Equal(ApiCompatibilityStatus.ProviderTooOld, range.Evaluate(new(1, 1, 9)));
        }

        [Fact]
        public void Evaluate_MaximumVersion_ReturnsProviderTooNew()
        {
            ApiVersionRange range = CreateRange();

            Assert.Equal(ApiCompatibilityStatus.ProviderTooNew, range.Evaluate(new(2, 0, 0)));
        }

        [Fact]
        public void Evaluate_SupportedVersion_ReturnsCompatible()
        {
            ApiVersionRange range = CreateRange();

            Assert.Equal(ApiCompatibilityStatus.Compatible, range.Evaluate(new(1, 5, 0)));
        }

        [Fact]
        public void ToString_BoundedRange_UsesIntervalNotation() 
            => Assert.Equal("[1.2.0, 2.0.0)", CreateRange().ToString());

        [Fact]
        public void ToString_UnboundedRange_UsesInfinity()
        {
            var range = new ApiVersionRange(new(1, 2, 0), null);

            Assert.Equal("[1.2.0, infinity)", range.ToString());
        }

        private static ApiVersionRange CreateRange() 
            => new(new(1, 2, 0), new(2, 0, 0));
    }
}