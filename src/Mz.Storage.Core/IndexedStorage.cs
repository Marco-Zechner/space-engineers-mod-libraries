using System;
using System.Collections.Generic;
using System.IO;

namespace Mz.Storage
{
    /// <summary>
    /// Provides indexed logical-name access over a storage backend.
    /// </summary>
    public sealed class IndexedStorage
    {
        /// <summary>
        /// Gets the reserved physical filename used for the persisted logical-name index.
        /// </summary>
        public const string IndexFileName = "__mz_storage_index_v1";

        private readonly IStorageBackend _backend;
        private readonly string _physicalPrefix;

        /// <summary>
        /// Creates indexed storage without a physical filename prefix.
        /// </summary>
        public IndexedStorage(IStorageBackend backend) : this(backend, string.Empty)
        {
        }

        /// <summary>
        /// Creates indexed storage with a prefix applied to every physical filename.
        /// </summary>
        public IndexedStorage(IStorageBackend backend, string physicalPrefix)
        {
            if (backend == null)
                throw new ArgumentNullException(nameof(backend));

            if (physicalPrefix == null)
                throw new ArgumentNullException(nameof(physicalPrefix));

            _backend = backend;
            _physicalPrefix = physicalPrefix;
        }

        /// <summary>
        /// Returns whether the logical file currently exists.
        /// </summary>
        public bool Exists(string name)
        {
            ValidateName(name);
            return _backend.Exists(GetPhysicalName(name));
        }

        /// <summary>
        /// Loads the complete logical file content and indexes a previously unknown existing file.
        /// </summary>
        public string Load(string name)
        {
            ValidateName(name);

            var physicalName = GetPhysicalName(name);
            if (!_backend.Exists(physicalName))
                throw new FileNotFoundException("The storage file does not exist.", name);

            var content = _backend.Read(physicalName);
            AddKnown(name);
            return content;
        }

        /// <summary>
        /// Saves complete logical file content and adds the logical name to the persisted index.
        /// </summary>
        public void Save(string name, string content)
        {
            ValidateName(name);

            if (content == null)
                throw new ArgumentNullException(nameof(content));

            _backend.Write(GetPhysicalName(name), content);
            AddKnown(name);
        }

        /// <summary>
        /// Returns known logical names after pruning entries whose physical files no longer exist.
        /// </summary>
        public string[] ListKnown()
        {
            var names = ReadIndex();
            var staleNames = new List<string>();

            foreach (var name in names)
            {
                if (!_backend.Exists(GetPhysicalName(name)))
                    staleNames.Add(name);
            }

            if (staleNames.Count > 0)
            {
                foreach (var name in staleNames)
                    names.Remove(name);

                WriteIndex(names);
            }

            return ToSortedArray(names);
        }

        private void AddKnown(string name)
        {
            var names = ReadIndex();
            if (!names.Add(name))
                return;

            WriteIndex(names);
        }

        private HashSet<string> ReadIndex()
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            var physicalIndexName = GetPhysicalIndexName();

            if (!_backend.Exists(physicalIndexName))
                return names;

            var content = _backend.Read(physicalIndexName);
            if (string.IsNullOrEmpty(content))
                return names;

            var entries = content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var entry in entries)
                names.Add(entry);

            return names;
        }

        private void WriteIndex(HashSet<string> names)
        {
            var sorted = ToSortedArray(names);
            var content = sorted.Length == 0 ? string.Empty : string.Join("\n", sorted) + "\n";
            _backend.Write(GetPhysicalIndexName(), content);
        }

        private static string[] ToSortedArray(HashSet<string> names)
        {
            var result = new string[names.Count];
            names.CopyTo(result);
            Array.Sort(result, StringComparer.Ordinal);
            return result;
        }

        private string GetPhysicalName(string name) => _physicalPrefix + name;

        private string GetPhysicalIndexName() => _physicalPrefix + IndexFileName;

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A logical storage name is required.", nameof(name));

            if (name == IndexFileName)
                throw new ArgumentException("The logical storage name is reserved by Mz.Storage.", nameof(name));

            if (name.IndexOf('\r') >= 0 || name.IndexOf('\n') >= 0)
                throw new ArgumentException("Logical storage names cannot contain line breaks.", nameof(name));
        }
    }
}
