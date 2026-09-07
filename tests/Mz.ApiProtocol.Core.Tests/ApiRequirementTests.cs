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
            ApiVersionRange range = CreateRange();

            var requirement = new ApiRequirement("  Mz.CommandAPI  ", range);

            Assert.Equal("Mz.CommandAPI", requirement.ApiId);
            Assert.Same(range, requirement.SupportedVersions);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidApiId_ThrowsArgumentException(string? apiId)
        {
            Assert.ThrowsAny<ArgumentException>(() =>
                {
                    new ApiRequirement(apiId!, CreateRange());
                }
            );
        }

        [Fact]
        public void Constructor_NullRange_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                {
                    new ApiRequirement("Mz.CommandAPI", null!);
                }
            );
        }

        [Fact]
        public void Evaluate_MatchingSupportedProvider_ReturnsCompatible()
        {
            ApiRequirement requirement = CreateRequirement();

            var provider = new ApiDescriptor("Mz.CommandAPI", new(1, 5, 0));

            Assert.Equal(ApiCompatibilityStatus.Compatible, requirement.Evaluate(provider));
            Assert.True(requirement.IsSatisfiedBy(provider));
        }

        [Fact]
        public void Evaluate_DifferentApiId_ReturnsDifferentApi()
        {
            ApiRequirement requirement = CreateRequirement();

            var provider = new ApiDescriptor("Mz.OtherAPI", new(1, 5, 0));

            Assert.Equal(ApiCompatibilityStatus.DifferentApi, requirement.Evaluate(provider));
            Assert.False(requirement.IsSatisfiedBy(provider));
        }

        [Fact]
        public void Evaluate_ApiIdComparisonIsCaseSensitive()
        {
            ApiRequirement requirement = CreateRequirement();

            var provider = new ApiDescriptor("mz.commandapi", new(1, 5, 0));

            Assert.Equal(ApiCompatibilityStatus.DifferentApi, requirement.Evaluate(provider));
        }

        [Fact]
        public void Evaluate_OldProvider_ReturnsProviderTooOld()
        {
            ApiRequirement requirement = CreateRequirement();

            var provider = new ApiDescriptor("Mz.CommandAPI", new(1, 1, 9));

            Assert.Equal(ApiCompatibilityStatus.ProviderTooOld, requirement.Evaluate(provider)
            );
        }

        [Fact]
        public void Evaluate_NewProvider_ReturnsProviderTooNew()
        {
            ApiRequirement requirement = CreateRequirement();

            var provider = new ApiDescriptor("Mz.CommandAPI", new(2, 0, 0));

            Assert.Equal(ApiCompatibilityStatus.ProviderTooNew, requirement.Evaluate(provider));
        }

        [Fact]
        public void Evaluate_NullProvider_ThrowsArgumentNullException()
        {
            ApiRequirement requirement = CreateRequirement();

            Assert.Throws<ArgumentNullException>(() => requirement.Evaluate(null!));
        }

        private static ApiRequirement CreateRequirement() 
            => new("Mz.CommandAPI", CreateRange());

        private static ApiVersionRange CreateRange() 
            => new(new(1, 2, 0), new(2, 0, 0));
    }
}