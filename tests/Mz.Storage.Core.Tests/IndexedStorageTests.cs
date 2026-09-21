using System.Collections.Generic;
using Xunit;

namespace Mz.Storage.Tests
{
    public sealed class IndexedStorageTests
    {
        [Fact]
        public void Save_WritesContentAndIndexesLogicalName()
        {
            var backend = new MemoryStorageBackend();
            var storage = new IndexedStorage(backend);

            storage.Save("config.toml", "value = 1");

            Assert.Equal("value = 1", backend.Files["config.toml"]);
            Assert.True(backend.Files.ContainsKey(IndexedStorage.IndexFileName));
            Assert.Equal(new[] { "config.toml" }, storage.ListKnown());
        }

        [Fact]
        public void Load_UnknownExistingFile_ReadsAndAddsItToIndex()
        {
            var backend = new MemoryStorageBackend();
            backend.Files["legacy.toml"] = "legacy = true";
            var storage = new IndexedStorage(backend);

            var content = storage.Load("legacy.toml");

            Assert.Equal("legacy = true", content);
            Assert.Equal(new[] { "legacy.toml" }, storage.ListKnown());
            Assert.True(backend.Files.ContainsKey(IndexedStorage.IndexFileName));
        }

        [Fact]
        public void ListKnown_NewInstance_LoadsPersistedIndex()
        {
            var backend = new MemoryStorageBackend();
            var first = new IndexedStorage(backend);
            first.Save("alpha.toml", "a");
            first.Save("beta.toml", "b");

            var second = new IndexedStorage(backend);

            Assert.Equal(new[] { "alpha.toml", "beta.toml" }, second.ListKnown());
        }

        [Fact]
        public void ListKnown_MissingIndexedFile_PrunesAndPersistsRepair()
        {
            var backend = new MemoryStorageBackend();
            var first = new IndexedStorage(backend);
            first.Save("keep.toml", "keep");
            first.Save("gone.toml", "gone");
            backend.Files.Remove("gone.toml");

            var repaired = new IndexedStorage(backend);
            Assert.Equal(new[] { "keep.toml" }, repaired.ListKnown());

            var reloaded = new IndexedStorage(backend);
            Assert.Equal(new[] { "keep.toml" }, reloaded.ListKnown());
        }

        [Fact]
        public void PhysicalPrefix_AppliesToDataAndIndexButNotLogicalNames()
        {
            var backend = new MemoryStorageBackend();
            var storage = new IndexedStorage(backend, "Mz.ConfigAPI.");

            storage.Save("server.toml", "enabled = true");

            Assert.Equal("enabled = true", backend.Files["Mz.ConfigAPI.server.toml"]);
            Assert.True(backend.Files.ContainsKey("Mz.ConfigAPI." + IndexedStorage.IndexFileName));
            Assert.Equal(new[] { "server.toml" }, storage.ListKnown());
        }

        [Fact]
        public void Exists_UnknownExistingFile_DoesNotMutateIndex()
        {
            var backend = new MemoryStorageBackend();
            backend.Files["known-by-caller.toml"] = "x";
            var storage = new IndexedStorage(backend);

            Assert.True(storage.Exists("known-by-caller.toml"));
            Assert.Empty(storage.ListKnown());
            Assert.False(backend.Files.ContainsKey(IndexedStorage.IndexFileName));
        }

        [Fact]
        public void ListKnown_CorruptEntries_AreIgnoredAndIndexIsRepaired()
        {
            var backend = new MemoryStorageBackend();
            backend.Files["alpha.toml"] = "a";
            backend.Files[IndexedStorage.IndexFileName] = "alpha.toml\n\n__mz_storage_index_v1\nalpha.toml\n";

            var storage = new IndexedStorage(backend);

            Assert.Equal(new[] { "alpha.toml" }, storage.ListKnown());
            Assert.Equal("alpha.toml\n", backend.Files[IndexedStorage.IndexFileName]);
        }

        [Fact]
        public void Load_MissingFile_ThrowsWithoutCreatingIndex()
        {
            var backend = new MemoryStorageBackend();
            var storage = new IndexedStorage(backend);

            Assert.Throws<System.IO.FileNotFoundException>(() => storage.Load("missing.toml"));
            Assert.False(backend.Files.ContainsKey(IndexedStorage.IndexFileName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("__mz_storage_index_v1")]
        [InlineData("bad\nname")]
        public void Save_InvalidLogicalName_DoesNotWrite(string? name)
        {
            var backend = new MemoryStorageBackend();
            var storage = new IndexedStorage(backend);

            Assert.ThrowsAny<System.ArgumentException>(() => storage.Save(name!, "content"));
            Assert.Empty(backend.Files);
        }

        [Fact]
        public void Save_NullContent_DoesNotWrite()
        {
            var backend = new MemoryStorageBackend();
            var storage = new IndexedStorage(backend);

            Assert.Throws<System.ArgumentNullException>(() => storage.Save("config.toml", null!));
            Assert.Empty(backend.Files);
        }
        private sealed class MemoryStorageBackend : IStorageBackend
        {
            public Dictionary<string, string> Files { get; } = new Dictionary<string, string>();

            public bool Exists(string fileName) => Files.ContainsKey(fileName);

            public string Read(string fileName) => Files[fileName];

            public void Write(string fileName, string content) => Files[fileName] = content;
        }
    }
}
