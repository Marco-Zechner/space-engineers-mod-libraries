using System;
using Xunit;

namespace Mz.Storage.SpaceEngineers.Tests
{
    public sealed class SpaceEngineersStorageTests
    {
        [Fact]
        public void CreateLocal_NullCallingType_Throws() =>
            Assert.Throws<ArgumentNullException>(() => SpaceEngineersStorage.CreateLocal(null!));

        [Fact]
        public void CreateWorld_NullCallingType_Throws() =>
            Assert.Throws<ArgumentNullException>(() => SpaceEngineersStorage.CreateWorld(null!));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(".")]
        [InlineData("...")]
        public void CreateGlobal_InvalidOwnerPrefix_Throws(string? ownerPrefix) =>
            Assert.ThrowsAny<ArgumentException>(() => SpaceEngineersStorage.CreateGlobal(ownerPrefix!));

        [Fact]
        public void FactoryMethods_DoNotRequireUtilitiesUntilStorageOperation()
        {
            Assert.NotNull(SpaceEngineersStorage.CreateLocal(typeof(SpaceEngineersStorageTests)));
            Assert.NotNull(SpaceEngineersStorage.CreateWorld(typeof(SpaceEngineersStorageTests)));
            Assert.NotNull(SpaceEngineersStorage.CreateGlobal("Mz.ConfigAPI"));
        }
    }
}