using System;

namespace Mz.SemanticVersioning
{
    /// <summary>
    /// Defines the released version and ordered changelog of this library
    /// package.
    /// </summary>
    public static class LibraryVersionFile
    {
        /// <summary>
        /// Gets the major version number.
        /// </summary>
        public const int Major = 0;

        /// <summary>
        /// Gets the minor version number.
        /// </summary>
        public const int Minor = 1;

        /// <summary>
        /// Gets the patch version number.
        /// </summary>
        public const int Patch = 1;

        /// <summary>
        /// Gets the version string.
        /// </summary>
        public static string VersionString => $"{Major}.{Minor}.{Patch}";

        /// <summary>
        /// Gets the complete changelog ordered from newest to oldest.
        /// </summary>
        public static ChangelogEntry[] Changelog => (ChangelogEntry[])Entries.Clone();

        private static readonly ChangelogEntry[] Entries =
        {
            new ChangelogEntry(
                "0.1.1",
                new[]
                {
                    "Added a complete package usage guide and installation examples."
                }
            ),
            new ChangelogEntry(
                "0.1.0",
                new[]
                {
                    "Published the initial SELibs package release."
                }
            )
        };
        
        /// <summary>
        /// Describes the changes published in one package version.
        /// </summary>
        public sealed class ChangelogEntry
        {
            private readonly string[] _changes;

            /// <summary>
            /// Gets the package version described by this entry.
            /// </summary>
            public string Version { get; }

            /// <summary>
            /// Gets a copy of the ordered changes for this version.
            /// </summary>
            public string[] Changes => (string[])_changes.Clone();

            /// <summary>
            /// Creates one immutable changelog entry.
            /// </summary>
            public ChangelogEntry(string version, string[] changes)
            {
                if (string.IsNullOrWhiteSpace(version))
                    throw new ArgumentException("A changelog version is required.", nameof(version));

                if (changes == null)
                    throw new ArgumentNullException(nameof(changes));

                Version = version;
                _changes = (string[])changes.Clone();
            }
        }
    }
}
