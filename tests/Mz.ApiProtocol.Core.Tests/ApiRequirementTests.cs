using System;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiRequirementTests
    {
        [Fact]
        public void Constructor_StoresNormalizedIdAndRange()
        {
            var range = CreateRange();

            var requirement = new ApiRequirement(
                "  Mz.CommandAPI  ",
                range
            );

            Assert.Equal("Mz.CommandAPI", requirement.ApiId);
            Assert.Same(range, requirement.SupportedVersions);
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
                    new ApiRequirement(
                        apiId!,
                        CreateRange()
                    );
                }
            );
        }

        [Fact]
        public void Constructor_NullRange_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    new ApiRequirement(
                        "Mz.CommandAPI",
                        null!
                    );
                }
            );
        }

        [Fact]
        public void Evaluate_MatchingSupportedProvider_ReturnsCompatible()
        {
            var requirement = CreateRequirement();

            var provider = new ApiDescriptor(
                "Mz.CommandAPI",
                new SemanticVersion(1, 5, 0)
            );

            Assert.Equal(
                ApiCompatibilityStatus.Compatible,
                requirement.Evaluate(provider)
            );

            Assert.True(
                requirement.IsSatisfiedBy(provider)
            );
        }

        [Fact]
        public void Evaluate_DifferentApiId_ReturnsDifferentApi()
        {
            var requirement = CreateRequirement();

            var provider = new ApiDescriptor(
                "Mz.OtherAPI",
                new SemanticVersion(1, 5, 0)
            );

            Assert.Equal(
                ApiCompatibilityStatus.DifferentApi,
                requirement.Evaluate(provider)
            );

            Assert.False(
                requirement.IsSatisfiedBy(provider)
            );
        }

        [Fact]
        public void Evaluate_ApiIdComparisonIsCaseSensitive()
        {
            var requirement = CreateRequirement();

            var provider = new ApiDescriptor(
                "mz.commandapi",
                new SemanticVersion(1, 5, 0)
            );

            Assert.Equal(
                ApiCompatibilityStatus.DifferentApi,
                requirement.Evaluate(provider)
            );
        }

        [Fact]
        public void Evaluate_OldProvider_ReturnsProviderTooOld()
        {
            var requirement = CreateRequirement();

            var provider = new ApiDescriptor(
                "Mz.CommandAPI",
                new SemanticVersion(1, 1, 9)
            );

            Assert.Equal(
                ApiCompatibilityStatus.ProviderTooOld,
                requirement.Evaluate(provider)
            );
        }

        [Fact]
        public void Evaluate_NewProvider_ReturnsProviderTooNew()
        {
            var requirement = CreateRequirement();

            var provider = new ApiDescriptor(
                "Mz.CommandAPI",
                new SemanticVersion(2, 0, 0)
            );

            Assert.Equal(
                ApiCompatibilityStatus.ProviderTooNew,
                requirement.Evaluate(provider)
            );
        }

        [Fact]
        public void Evaluate_NullProvider_ThrowsArgumentNullException()
        {
            var requirement = CreateRequirement();

            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    requirement.Evaluate(null!);
                }
            );
        }

        private static ApiRequirement CreateRequirement()
        {
            return new ApiRequirement(
                "Mz.CommandAPI",
                CreateRange()
            );
        }

        private static ApiVersionRange CreateRange()
        {
            return new ApiVersionRange(
                new SemanticVersion(1, 2, 0),
                new SemanticVersion(2, 0, 0)
            );
        }
    }
}