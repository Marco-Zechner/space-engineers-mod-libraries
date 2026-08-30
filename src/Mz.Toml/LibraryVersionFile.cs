using Mz.SemanticVersioning;

namespace Mz.Toml
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
        public const int Patch = 0;

        /// <summary>
        /// Gets the version string.
        /// </summary>
        public static string VersionString => $"{Major}.{Minor}.{Patch}";

        /// <summary>
        /// Gets the complete changelog ordered from newest to oldest.
        /// </summary>
        public static Changelog Changelog { get; } = new Changelog(
            VersionString,
            new[]
            {
                new ChangelogEntry(
                    "0.1.0",
                    new[]
                    {
                        "Published the initial strict TOML 1.0 source library.",
                        "Added parsing, diagnostics, a mutable document model, and deterministic canonical writing.",
                        "Added strict UTF-8 byte parsing and complete pinned TOML 1.0 valid/invalid corpus validation.",
                        "Added permanent Space Engineers C# 6 source-copy validation.",
                        "Adopted the shared Mz.SemanticVersioning package metadata model."
                    }
                )
            }
        );
    }
}