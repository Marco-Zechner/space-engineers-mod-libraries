using System;
using Xunit;

namespace Mz.SemanticVersioning.Tests
{
    public sealed class SemanticVersionComparisonTests
    {
        [Fact]
        public void Constructor_StoresComponents()
        {
            var version = new SemanticVersion(10, 20, 30);

            Assert.Equal(10, version.Major);
            Assert.Equal(20, version.Minor);
            Assert.Equal(30, version.Patch);
        }

        [Theory]
        [InlineData(-1, 0, 0, "major")]
        [InlineData(0, -1, 0, "minor")]
        [InlineData(0, 0, -1, "patch")]
        public void Constructor_NegativeComponent_ThrowsArgumentOutOfRangeException(
            int major,
            int minor,
            int patch,
            string expectedParameterName
        )
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                delegate
                {
                    new SemanticVersion(major, minor, patch);
                }
            );

            Assert.Equal(expectedParameterName, exception.ParamName);
        }

        [Fact]
        public void Equals_SameComponents_ReturnsTrue()
        {
            var left = new SemanticVersion(1, 2, 3);
            var right = new SemanticVersion(1, 2, 3);

            Assert.True(left.Equals(right));
            Assert.True(left.Equals((object)right));
            Assert.Equal(left, right);
        }

        [Theory]
        [InlineData(2, 2, 3)]
        [InlineData(1, 3, 3)]
        [InlineData(1, 2, 4)]
        public void Equals_DifferentComponent_ReturnsFalse(
            int major,
            int minor,
            int patch
        )
        {
            var left = new SemanticVersion(1, 2, 3);
            var right = new SemanticVersion(major, minor, patch);

            Assert.False(left.Equals(right));
            Assert.NotEqual(left, right);
        }

        [Fact]
        public void Equals_NullOrDifferentType_ReturnsFalse()
        {
            var version = new SemanticVersion(1, 2, 3);

            Assert.False(version.Equals(null));
            Assert.False(version.Equals("1.2.3"));
        }

        [Fact]
        public void GetHashCode_EqualVersions_ReturnsSameHashCode()
        {
            var left = new SemanticVersion(1, 2, 3);
            var right = new SemanticVersion(1, 2, 3);

            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }

        [Theory]
        [InlineData(1, 0, 0, 2, 0, 0)]
        [InlineData(1, 1, 9, 1, 2, 0)]
        [InlineData(1, 2, 3, 1, 2, 4)]
        public void CompareTo_LowerVersion_ReturnsNegative(
            int leftMajor,
            int leftMinor,
            int leftPatch,
            int rightMajor,
            int rightMinor,
            int rightPatch
        )
        {
            var left = new SemanticVersion(
                leftMajor,
                leftMinor,
                leftPatch
            );

            var right = new SemanticVersion(
                rightMajor,
                rightMinor,
                rightPatch
            );

            Assert.True(left.CompareTo(right) < 0);
        }

        [Theory]
        [InlineData(2, 0, 0, 1, 9, 9)]
        [InlineData(1, 2, 0, 1, 1, 9)]
        [InlineData(1, 2, 4, 1, 2, 3)]
        public void CompareTo_HigherVersion_ReturnsPositive(
            int leftMajor,
            int leftMinor,
            int leftPatch,
            int rightMajor,
            int rightMinor,
            int rightPatch
        )
        {
            var left = new SemanticVersion(
                leftMajor,
                leftMinor,
                leftPatch
            );

            var right = new SemanticVersion(
                rightMajor,
                rightMinor,
                rightPatch
            );

            Assert.True(left.CompareTo(right) > 0);
        }

        [Fact]
        public void CompareTo_EqualVersion_ReturnsZero()
        {
            var left = new SemanticVersion(1, 2, 3);
            var right = new SemanticVersion(1, 2, 3);

            Assert.Equal(0, left.CompareTo(right));
        }

        [Fact]
        public void CompareTo_Null_ReturnsPositive()
        {
            var version = new SemanticVersion(1, 2, 3);

            Assert.True(version.CompareTo(null) > 0);
        }

        [Fact]
        public void EqualityOperators_UseValueEquality()
        {
            var left = new SemanticVersion(1, 2, 3);
            var equal = new SemanticVersion(1, 2, 3);
            var different = new SemanticVersion(1, 2, 4);

            Assert.True(left == equal);
            Assert.False(left != equal);

            Assert.False(left == different);
            Assert.True(left != different);
        }

        [Fact]
        public void EqualityOperators_HandleNull()
        {
            SemanticVersion? left = null;
            SemanticVersion? right = null;
            var version = new SemanticVersion(1, 2, 3);

            Assert.True(left == right);
            Assert.False(left != right);

            Assert.False(version == null);
            Assert.True(version != null);

            Assert.False(null == version);
            Assert.True(null != version);
        }

        [Fact]
        public void RelationalOperators_UseVersionOrdering()
        {
            var lower = new SemanticVersion(1, 2, 3);
            var equal = new SemanticVersion(1, 2, 3);
            var higher = new SemanticVersion(1, 3, 0);

            Assert.True(lower < higher);
            Assert.True(lower <= higher);
            Assert.False(lower > higher);
            Assert.False(lower >= higher);

            Assert.False(lower < equal);
            Assert.True(lower <= equal);
            Assert.False(lower > equal);
            Assert.True(lower >= equal);

            Assert.True(higher > lower);
            Assert.True(higher >= lower);
        }

        [Fact]
        public void RelationalOperators_TreatNullAsLowerThanVersion()
        {
            SemanticVersion? missing = null;
            var version = new SemanticVersion(1, 0, 0);

            Assert.True(missing < version);
            Assert.True(missing <= version);
            Assert.False(missing > version);
            Assert.False(missing >= version);

            Assert.False(version < missing);
            Assert.False(version <= missing);
            Assert.True(version > missing);
            Assert.True(version >= missing);
        }

        [Fact]
        public void RelationalOperators_TwoNullValuesAreEqual()
        {
            SemanticVersion? left = null;
            SemanticVersion? right = null;

            Assert.False(left < right);
            Assert.True(left <= right);
            Assert.False(left > right);
            Assert.True(left >= right);
        }
    }
}