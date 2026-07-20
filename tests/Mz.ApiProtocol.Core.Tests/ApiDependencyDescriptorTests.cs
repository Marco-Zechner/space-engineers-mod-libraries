using System;
using Mz.SemanticVersioning;
using Xunit;

namespace Mz.ApiProtocol.Tests
{
    public sealed class ApiDependencyDescriptorTests
    {
        [Fact]
        public void Constructor_StoresRequiredDependency()
        {
            ApiModIdentity consumer = CreateConsumer();
            ApiRequirement requirement = CreateRequirement();

            var dependency = new ApiDependencyDescriptor(
                consumer,
                requirement,
                ApiDependencyKind.Required,
                "  Registers chat commands  "
            );

            Assert.Same(consumer, dependency.Consumer);
            Assert.Same(requirement, dependency.Requirement);

            Assert.Equal(
                ApiDependencyKind.Required,
                dependency.Kind
            );

            Assert.Equal(
                "Registers chat commands",
                dependency.FeatureDescription
            );

            Assert.True(dependency.IsRequired);
            Assert.False(dependency.IsOptional);
        }

        [Fact]
        public void Constructor_StoresOptionalDependency()
        {
            var dependency = new ApiDependencyDescriptor(
                CreateConsumer(),
                CreateRequirement(),
                ApiDependencyKind.Optional,
                null!
            );

            Assert.Equal(
                string.Empty,
                dependency.FeatureDescription
            );

            Assert.False(dependency.IsRequired);
            Assert.True(dependency.IsOptional);
        }

        [Fact]
        public void Constructor_NullConsumer_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    new ApiDependencyDescriptor(
                        null!,
                        CreateRequirement(),
                        ApiDependencyKind.Required,
                        string.Empty
                    );
                }
            );
        }

        [Fact]
        public void Constructor_NullRequirement_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    new ApiDependencyDescriptor(
                        CreateConsumer(),
                        null!,
                        ApiDependencyKind.Required,
                        string.Empty
                    );
                }
            );
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        [InlineData(100)]
        public void Constructor_InvalidKind_ThrowsArgumentException(
            int numericKind
        )
        {
            Assert.Throws<ArgumentException>(
                delegate
                {
                    new ApiDependencyDescriptor(
                        CreateConsumer(),
                        CreateRequirement(),
                        (ApiDependencyKind)numericKind,
                        string.Empty
                    );
                }
            );
        }

        private static ApiModIdentity CreateConsumer()
        {
            return new ApiModIdentity(
                "Mz.ConsumerMod",
                "Consumer Mod",
                new SemanticVersion(2, 1, 0)
            );
        }

        private static ApiRequirement CreateRequirement()
        {
            return new ApiRequirement(
                "Mz.CommandAPI",
                new ApiVersionRange(
                    new SemanticVersion(1, 0, 0),
                    new SemanticVersion(2, 0, 0)
                )
            );
        }
    }
}